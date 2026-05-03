"use client";

import { useState } from "react";
import { AppSidebar } from "@/components/app-sidebar";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { Separator } from "@/components/ui/separator";
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Save, Bell, Lock, User } from "lucide-react";
import Link from "next/link";
import { ThemeToggle } from "@/components/dashboard/theme-toggle";

export default function SettingsPage() {
  const [formData, setFormData] = useState({
    companyName: "Sett",
    email: "admin@sett.com",
    phone: "+34 ",
  });

  const [notifications, setNotifications] = useState({
    emailAlerts: true,
    systemNotifications: true,
    weeklyReport: false,
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleToggle = (key: keyof typeof notifications) => {
    setNotifications((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleSave = () => {
    console.log("Guardando configuración:", formData);
  };

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset className="bg-background">
        <header className="flex h-14 shrink-0 items-center gap-2 border-b border-border px-4 bg-surface">
          <SidebarTrigger className="-ml-1" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <span className="text-muted-foreground text-sm">Sett</span>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block" />
              <BreadcrumbItem>
                <BreadcrumbPage className="text-foreground text-sm">
                  Settings
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
          <div className="ml-auto">
            <ThemeToggle />
          </div>
        </header>

        <div className="flex flex-1 flex-col gap-6 p-6 items-center">
          <div className="max-w-2xl w-full flex flex-col gap-5">
            <Card className="bg-card border border-border">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <User className="size-5 text-muted-foreground" />
                  <CardTitle className="text-foreground text-base">
                    Información general
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <Label className="text-foreground mb-2 block text-sm">
                    Nombre de la empresa
                  </Label>
                  <Input
                    type="text"
                    name="companyName"
                    value={formData.companyName}
                    onChange={handleInputChange}
                    className="bg-muted border-border text-foreground"
                  />
                </div>
                <div>
                  <Label className="text-foreground mb-2 block text-sm">
                    Email
                  </Label>
                  <Input
                    type="email"
                    name="email"
                    value={formData.email}
                    onChange={handleInputChange}
                    className="bg-muted border-border text-foreground"
                  />
                </div>
                <div>
                  <Label className="text-foreground mb-2 block text-sm">
                    Teléfono
                  </Label>
                  <Input
                    type="tel"
                    name="phone"
                    value={formData.phone}
                    onChange={handleInputChange}
                    className="bg-muted border-border text-foreground"
                  />
                </div>
              </CardContent>
            </Card>

            <Card className="bg-card border border-border">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Bell className="size-5 text-muted-foreground" />
                  <CardTitle className="text-foreground text-base">
                    Notificaciones
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                {[
                  {
                    key: "emailAlerts" as const,
                    title: "Alertas por email",
                    desc: "Recibe alertas importantes por correo",
                  },
                  {
                    key: "systemNotifications" as const,
                    title: "Notificaciones del sistema",
                    desc: "Recibe notificaciones en tiempo real",
                  },
                  {
                    key: "weeklyReport" as const,
                    title: "Reporte semanal",
                    desc: "Resumen semanal de actividades",
                  },
                ].map((item) => (
                  <div
                    key={item.key}
                    className="flex items-center justify-between p-3 bg-muted rounded-md border border-border"
                  >
                    <div>
                      <p className="text-foreground font-medium text-sm">
                        {item.title}
                      </p>
                      <p className="text-muted-foreground text-xs">
                        {item.desc}
                      </p>
                    </div>
                    <input
                      type="checkbox"
                      checked={notifications[item.key]}
                      onChange={() => handleToggle(item.key)}
                      className="size-4 cursor-pointer accent-[var(--accent-active)]"
                    />
                  </div>
                ))}
              </CardContent>
            </Card>

            <Card className="bg-card border border-border">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Lock className="size-5 text-muted-foreground" />
                  <CardTitle className="text-foreground text-base">
                    Seguridad
                  </CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-3">
                <Button
                  asChild
                  variant="outline"
                  className="w-full justify-start"
                >
                  <Link href="/forgot">Cambiar contraseña</Link>
                </Button>
                <Button
                  asChild
                  variant="outline"
                  className="w-full justify-start"
                >
                  <Link href="/login">Cerrar sesión</Link>
                </Button>
              </CardContent>
            </Card>

            <div className="flex gap-3">
              <Button
                onClick={handleSave}
                className="flex-1 bg-foreground text-background hover:bg-foreground/90 flex items-center justify-center gap-2"
              >
                <Save className="size-4" />
                Guardar cambios
              </Button>
            </div>
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  );
}
