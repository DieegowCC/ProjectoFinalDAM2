"use client"

import * as React from "react"

import { SearchForm } from "@/components/search-form"
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from "@/components/ui/sidebar"
import { House } from "lucide-react"

const data = {
  navMain: [
    {
      title: "Home",
      url: "/dashboard",
    },
    {
      title: "Settings",
      url: "/settings",
    },
    {
      title: "Architecture",
      url: "#",
    },
    {
      title: "Community",
      url: "#",
    },
  ],
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar className="bg-[#1a1a1a] border-r border-[#2e2e2e]" {...props}>
      <SidebarHeader className="border-b border-[#2e2e2e]">
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
              <a href="#">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-gray-600 text-white">
                  <House className="size-4" />
                </div>
                <div className="flex flex-col gap-0.5 leading-none">
                  <span className="font-medium text-white">Sett</span>
                  <span className="text-gray-400 text-xs">v0.0.2</span>
                </div>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
        <SearchForm />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navMain.map((item) => (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton
                  asChild
                  className="text-gray-300 hover:bg-gray-700 hover:text-white"
                >
                  <a href={item.url}>{item.title}</a>
                </SidebarMenuButton>
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarRail className="bg-[#1a1a1a] hover:bg-[#2a2a2a]" />
    </Sidebar>
  )
}