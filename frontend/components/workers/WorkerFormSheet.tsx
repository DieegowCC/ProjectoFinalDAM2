"use client";

import { useEffect, useState } from "react";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export type WorkerFormData = {
  id?: number;
  name: string;
  email: string;
  hostname: string;
  department: string | null;
  is_active: boolean;
};

type WorkerFormSheetProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialData: WorkerFormData | null; // null = crear, objeto = editar
  onSaved: () => void;
};

const empty: WorkerFormData = {
  name: "",
  email: "",
  hostname: "",
  department: "",
  is_active: true,
};

export function WorkerFormSheet({
  open,
  onOpenChange,
  initialData,
  onSaved,
}: WorkerFormSheetProps) {
  const [form, setForm] = useState<WorkerFormData>(empty);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (open) {
      setForm(initialData ?? empty);
      setError(null);
    }
  }, [open, initialData]);

  const isEditing = Boolean(initialData?.id);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      const token = localStorage.getItem("token");
      const apiUrl = process.env.NEXT_PUBLIC_API_URL;
      const url = isEditing
        ? `${apiUrl}/api/workers/${form.id}`
        : `${apiUrl}/api/workers`;
      const res = await fetch(url, {
        method: isEditing ? "PUT" : "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          ...(isEditing ? { id: form.id } : {}),
          name: form.name,
          email: form.email,
          hostname: form.hostname,
          department: form.department || null,
          is_active: form.is_active,
        }),
      });
      if (!res.ok) {
        const txt = await res.text();
        setError(txt || `HTTP ${res.status}`);
        return;
      }
      onSaved();
      onOpenChange(false);
    } catch (err) {
      setError(String(err));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-card sm:max-w-md">
        <SheetHeader>
          <SheetTitle className="text-foreground">
            {isEditing ? "Editar worker" : "Nuevo worker"}
          </SheetTitle>
          <SheetDescription>
            {isEditing
              ? "Modifica los datos del worker."
              : "Crea un nuevo worker. El hostname debe coincidir con el equipo donde correrá el agente."}
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={handleSubmit} className="px-4 pb-4 flex flex-col gap-4">
          <div className="flex flex-col gap-2">
            <Label htmlFor="name" className="text-foreground text-sm">
              Nombre
            </Label>
            <Input
              id="name"
              name="name"
              value={form.name}
              onChange={handleChange}
              required
              className="bg-muted border-border text-foreground"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="email" className="text-foreground text-sm">
              Email
            </Label>
            <Input
              id="email"
              name="email"
              type="email"
              value={form.email}
              onChange={handleChange}
              required
              className="bg-muted border-border text-foreground"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="hostname" className="text-foreground text-sm">
              Hostname
            </Label>
            <Input
              id="hostname"
              name="hostname"
              value={form.hostname}
              onChange={handleChange}
              required
              disabled={isEditing}
              className="bg-muted border-border text-foreground font-mono"
            />
            {isEditing && (
              <span className="text-xs text-muted-foreground">
                El hostname no puede modificarse.
              </span>
            )}
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="department" className="text-foreground text-sm">
              Departamento
            </Label>
            <Input
              id="department"
              name="department"
              value={form.department ?? ""}
              onChange={handleChange}
              className="bg-muted border-border text-foreground"
            />
          </div>
          <label className="flex items-center gap-2 text-sm text-foreground">
            <input
              type="checkbox"
              checked={form.is_active}
              onChange={(e) =>
                setForm((p) => ({ ...p, is_active: e.target.checked }))
              }
              className="accent-[var(--accent-active)]"
            />
            Activo
          </label>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <SheetFooter className="px-0">
            <Button
              type="submit"
              disabled={saving}
              className="bg-foreground text-background hover:bg-foreground/90"
            >
              {saving ? "Guardando…" : isEditing ? "Guardar cambios" : "Crear"}
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
            >
              Cancelar
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
