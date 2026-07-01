import { useCallback, useEffect, useState } from "react";
import { Server, Plus, ArrowLeft, Trash2 } from "lucide-react";
import { api, timeAgo, type Service, type ServiceItem } from "../lib/api";
import { useOrg } from "../lib/useOrg";
import { Badge, Card, EmptyState, Field, Modal, PageHeader, PrimaryButton, SecondaryButton, Table } from "../components/ui";

export default function Services() {
  const org = useOrg();
  const [services, setServices] = useState<Service[]>([]);
  const [selected, setSelected] = useState<string | null>(null);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState({ name: "", description: "" });
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => { api.services(org).then((r) => setServices(r.services ?? [])); }, [org]);
  useEffect(() => { load(); }, [load]);

  async function create() {
    if (!form.name) return;
    setError(null);
    try { await api.createService(org, form.name, form.description); setShowNew(false); setForm({ name: "", description: "" }); load(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  }

  if (selected) {
    return <ServiceDetail org={org} name={selected}
      onBack={() => setSelected(null)}
      onDeleted={() => { setSelected(null); load(); }} />;
  }

  return (
    <div>
      <PageHeader icon={Server} title="Services"
        actions={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>New service</PrimaryButton>} />
      <Table
        rows={services}
        onRowClick={(s) => setSelected(s.name)}
        columns={[
          { header: "Service", cell: (s) => <span className="font-medium">{s.name}</span> },
          { header: "Description", cell: (s) => <span className="text-ink-dim">{s.description || "—"}</span> },
          { header: "Created", cell: (s) => <span className="text-ink-faint">{timeAgo(s.created)}</span> },
        ]}
        empty={<EmptyState icon={Server} title="Create your first service"
          description="Services group stacks, environments and other resources in a way that makes sense to your organization."
          action={<PrimaryButton icon={Plus} onClick={() => setShowNew(true)}>Create a new service</PrimaryButton>} />}
      />
      {showNew && (
        <Modal title="Create a service" onClose={() => setShowNew(false)}
          footer={<><SecondaryButton onClick={() => setShowNew(false)}>Cancel</SecondaryButton><PrimaryButton onClick={create}>Create</PrimaryButton></>}>
          <Field label="Name" value={form.name} onChange={(v) => setForm((f) => ({ ...f, name: v }))} placeholder="checkout" />
          <Field label="Description" value={form.description} onChange={(v) => setForm((f) => ({ ...f, description: v }))} placeholder="Checkout service stacks + environments" />
          {error && <p className="text-xs text-red-400">{error}</p>}
        </Modal>
      )}
    </div>
  );
}

function ServiceDetail({ org, name, onBack, onDeleted }: {
  org: string; name: string; onBack: () => void; onDeleted: () => void;
}) {
  const [description, setDescription] = useState("");
  const [items, setItems] = useState<ServiceItem[]>([]);
  const [editing, setEditing] = useState(false);
  const [adding, setAdding] = useState(false);

  const reload = useCallback(() => {
    api.getService(org, name).then((r) => { setDescription(r.service.description ?? ""); setItems(r.items ?? []); });
  }, [org, name]);
  useEffect(() => { reload(); }, [reload]);

  const remove = async (item: ServiceItem) => { await api.removeServiceItem(org, name, item.type, item.name); reload(); };
  const removeService = async () => { await api.deleteService(org, name); onDeleted(); };

  return (
    <div>
      <PageHeader icon={Server} title={name}
        actions={<SecondaryButton icon={ArrowLeft} onClick={onBack}>Back to services</SecondaryButton>} />
      <div className="mt-4 space-y-4">
        <Card title="Metadata" actions={<SecondaryButton onClick={() => setEditing(true)}>Edit description</SecondaryButton>}>
          <p className="px-6 py-4 text-sm text-ink-dim">{description || "No description."}</p>
        </Card>
        <Card title="Items" actions={<PrimaryButton icon={Plus} onClick={() => setAdding(true)}>Add item</PrimaryButton>}>
          <Table<ServiceItem>
            rows={items}
            empty={<p className="px-6 py-4 text-sm text-ink-dim">No items yet. Add stacks, environments or other resources.</p>}
            columns={[
              { header: "Type", cell: (i) => <Badge tone="brand">{i.type}</Badge> },
              { header: "Name", cell: (i) => <span className="font-medium">{i.name}</span> },
              { header: "Added", cell: (i) => <span className="text-ink-faint">{timeAgo(i.created)}</span> },
              {
                header: "", className: "text-right",
                cell: (i) => (
                  <button onClick={() => remove(i)} className="text-red-400 hover:text-red-300">Remove</button>
                ),
              },
            ]}
          />
        </Card>
        <button onClick={removeService} className="inline-flex items-center gap-2 text-sm text-red-400 hover:text-red-300">
          <Trash2 className="h-4 w-4" /> Delete this service
        </button>
      </div>
      {editing && (
        <EditDescriptionModal org={org} name={name} current={description}
          onClose={() => setEditing(false)}
          onSaved={() => { setEditing(false); reload(); }} />
      )}
      {adding && (
        <AddItemModal org={org} name={name}
          onClose={() => setAdding(false)}
          onAdded={() => { setAdding(false); reload(); }} />
      )}
    </div>
  );
}

function EditDescriptionModal({ org, name, current, onClose, onSaved }: {
  org: string; name: string; current: string; onClose: () => void; onSaved: () => void;
}) {
  const [description, setDescription] = useState(current);
  const [error, setError] = useState<string | null>(null);

  const save = async () => {
    setError(null);
    try { await api.updateService(org, name, description); onSaved(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  };

  return (
    <Modal title="Edit description" onClose={onClose}
      footer={<><SecondaryButton onClick={onClose}>Cancel</SecondaryButton><PrimaryButton onClick={save}>Save</PrimaryButton></>}>
      <Field label="Description" value={description} onChange={setDescription} placeholder="What this service groups" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}

function AddItemModal({ org, name, onClose, onAdded }: {
  org: string; name: string; onClose: () => void; onAdded: () => void;
}) {
  const [type, setType] = useState("stack");
  const [itemName, setItemName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const save = async () => {
    if (!itemName.trim()) return;
    setError(null);
    try { await api.addServiceItems(org, name, [{ type, name: itemName }]); onAdded(); }
    catch (e) { setError(e instanceof Error ? e.message : String(e)); }
  };

  return (
    <Modal title="Add item" onClose={onClose}
      footer={<><SecondaryButton onClick={onClose}>Cancel</SecondaryButton><PrimaryButton onClick={save}>Add</PrimaryButton></>}>
      <Field label="Type" value={type} onChange={setType} options={["stack", "environment", "template"]} />
      <Field label="Name" value={itemName} onChange={setItemName} placeholder="checkout-prod" />
      {error && <p className="text-xs text-red-400">{error}</p>}
    </Modal>
  );
}
