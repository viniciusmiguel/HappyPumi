import { useEffect, useState } from "react";
import { api } from "./api";

/** The current organization slug (first org of the signed-in user; "happypumi" in the seeded backend). */
export function useOrg(): string {
  const [org, setOrg] = useState<string>("happypumi");
  useEffect(() => {
    api.currentUser().then((u) => {
      const first = u.organizations?.[0]?.githubLogin ?? u.githubLogin;
      if (first) setOrg(first);
    });
  }, []);
  return org;
}
