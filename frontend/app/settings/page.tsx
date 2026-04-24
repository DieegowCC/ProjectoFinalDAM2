"use client"

import { useState } from "react"
import { AppSidebar } from "@/components/app-sidebar"
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import { Separator } from "@/components/ui/separator"
import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { LogIn, Save, Bell, Lock, User } from "lucide-react"
import Link from "next/link"

export default function SettingsPage() {
  const [formData, setFormData] = useState({
    companyName: "Sett",
    email: "admin@sett.com",
    phone: "+34 ",
  })

  const [notifications, setNotifications] = useState({
    emailAlerts: true,
    systemNotifications: true,
    weeklyReport: false,
  })

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleToggle = (key: keyof typeof notifications) => {
    setNotifications((prev) => ({ ...prev, [key]: !prev[key] }))
  }

  const handleSave = () => {
    console.log("Guardando configuración:", formData)
  }

  return (
    <SidebarProvider
      style={{ fontFamily: "Verdana, Geneva, Tahoma, sans-serif" }}
    >
      <AppSidebar />
      <SidebarInset className="bg-gray-950">
        <header className="flex h-16 shrink-0 items-center gap-2 border-b border-gray-800 px-4 bg-gray-900">
          <SidebarTrigger className="-ml-1 text-gray-300 hover:text-white hover:bg-gray-800" />
          <Separator
            orientation="vertical"
            className="mr-2 data-[orientation=vertical]:h-4 bg-gray-700"
          />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem className="hidden md:block">
                <BreadcrumbLink href="#" className="text-gray-400 hover:text-white">
                  Sett
                </BreadcrumbLink>
              </BreadcrumbItem>
              <BreadcrumbSeparator className="hidden md:block text-gray-600" />
              <BreadcrumbItem>
                <BreadcrumbPage className="text-white">
                  Settings
                </BreadcrumbPage>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </header>

        <div className="flex flex-1 flex-col gap-6 p-6 items-center justify-center">
          <div className="max-w-2xl w-full">
            {/* Información General */}
            <Card className="bg-gray-800 border-gray-700 mb-6">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <User className="size-5 text-gray-400" />
                  <CardTitle className="text-white">Información General</CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <Label className="text-gray-300 mb-2 block">Nombre de la Empresa</Label>
                  <Input
                    type="text"
                    name="companyName"
                    value={formData.companyName}
                    onChange={handleInputChange}
                    className="bg-gray-700 border-gray-600 text-white placeholder:text-gray-500 focus:border-gray-400 focus:ring-gray-400"
                  />
                </div>
                <div>
                  <Label className="text-gray-300 mb-2 block">Email</Label>
                  <Input
                    type="email"
                    name="email"
                    value={formData.email}
                    onChange={handleInputChange}
                    className="bg-gray-700 border-gray-600 text-white placeholder:text-gray-500 focus:border-gray-400 focus:ring-gray-400"
                  />
                </div>
                <div>
                  <Label className="text-gray-300 mb-2 block">Teléfono</Label>
                  <Input
                    type="tel"
                    name="phone"
                    value={formData.phone}
                    onChange={handleInputChange}
                    className="bg-gray-700 border-gray-600 text-white placeholder:text-gray-500 focus:border-gray-400 focus:ring-gray-400"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Notificaciones */}
            <Card className="bg-gray-800 border-gray-700 mb-6">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Bell className="size-5 text-gray-400" />
                  <CardTitle className="text-white">Notificaciones</CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center justify-between p-3 bg-gray-700 rounded-lg">
                  <div>
                    <p className="text-white font-medium">Alertas por Email</p>
                    <p className="text-gray-400 text-sm">Recibe alertas importantes por correo</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={notifications.emailAlerts}
                    onChange={() => handleToggle("emailAlerts")}
                    className="w-5 h-5 cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between p-3 bg-gray-700 rounded-lg">
                  <div>
                    <p className="text-white font-medium">Notificaciones del Sistema</p>
                    <p className="text-gray-400 text-sm">Recibe notificaciones en tiempo real</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={notifications.systemNotifications}
                    onChange={() => handleToggle("systemNotifications")}
                    className="w-5 h-5 cursor-pointer"
                  />
                </div>

                <div className="flex items-center justify-between p-3 bg-gray-700 rounded-lg">
                  <div>
                    <p className="text-white font-medium">Reporte Semanal</p>
                    <p className="text-gray-400 text-sm">Recibe un resumen semanal de actividades</p>
                  </div>
                  <input
                    type="checkbox"
                    checked={notifications.weeklyReport}
                    onChange={() => handleToggle("weeklyReport")}
                    className="w-5 h-5 cursor-pointer"
                  />
                </div>
              </CardContent>
            </Card>

            {/* Seguridad */}
            <Card className="bg-gray-800 border-gray-700 mb-6">
              <CardHeader>
                <div className="flex items-center gap-2">
                  <Lock className="size-5 text-gray-400" />
                  <CardTitle className="text-white">Seguridad</CardTitle>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <Button className="w-full bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors">
                <Link href="/forgot">
                Cambiar contraseña
                </Link>
                </Button>
                <Button className="w-full bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors">
                <Link href="/login">
                Cerrar Sesion
                </Link>
                </Button>
              </CardContent>
            </Card>

            {/* Botón Guardar */}
            <div className="flex gap-3">
              <Button
                onClick={handleSave}
                className="flex-1 bg-gray-600 text-white hover:bg-gray-500 border-0 transition-colors flex items-center justify-center gap-2"
              >
                <Save className="size-4" />
                Guardar Cambios
              </Button>
            </div>
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}