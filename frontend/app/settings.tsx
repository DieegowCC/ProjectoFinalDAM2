"use client"
import { createContext, useContext, useState } from "react";

const SettingsContext = createContext({
  fontSize: "text-base",
  setFontSize: (size: string) => {},
});

export function SettingsProvider({ children }: { children: React.ReactNode }) {
  const [fontSize, setFontSize] = useState("text-base");
  return (
    <SettingsContext.Provider value={{ fontSize, setFontSize }}>
      <div className={fontSize}>{children}</div>
    </SettingsContext.Provider>
  );
}

export const useSettings = () => useContext(SettingsContext);