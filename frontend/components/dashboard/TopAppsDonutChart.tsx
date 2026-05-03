"use client";

import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";

export type TopAppDto = {
  name: string;
  minutes: number;
};

type TopAppsDonutChartProps = {
  apps: TopAppDto[];
};

const COLORS = ["#6EC531", "#3b82f6", "#8b5cf6", "#f59e0b", "#ec4899", "#52525b"];

function formatMinutes(minutes: number): string {
  if (minutes <= 0) return "0m";
  const h = Math.floor(minutes / 60);
  const m = minutes % 60;
  if (h === 0) return `${m}m`;
  return `${h}h ${m}m`;
}

export function TopAppsDonutChart({ apps }: TopAppsDonutChartProps) {
  const total = apps.reduce((acc, a) => acc + a.minutes, 0);

  return (
    <div className="rounded-[var(--radius)] border border-border bg-card p-5 flex flex-col">
      <h3 className="text-sm font-medium text-foreground">Top apps hoy</h3>
      <p className="text-xs text-muted-foreground mt-0.5 mb-3">
        Tiempo de uso por aplicación
      </p>

      <div className="h-44">
        {total === 0 ? (
          <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
            Sin datos
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={apps}
                innerRadius={50}
                outerRadius={75}
                paddingAngle={2}
                dataKey="minutes"
                nameKey="name"
                stroke="none"
              >
                {apps.map((_, i) => (
                  <Cell key={i} fill={COLORS[i % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip
                contentStyle={{
                  backgroundColor: "var(--card)",
                  border: "1px solid var(--border)",
                  borderRadius: "0.5rem",
                  fontSize: "0.75rem",
                  color: "var(--foreground)",
                }}
                formatter={(value, name) => [
                  formatMinutes(Number(value) || 0),
                  String(name),
                ]}
              />
            </PieChart>
          </ResponsiveContainer>
        )}
      </div>

      <div className="flex flex-col gap-1.5 mt-4 text-xs">
        {apps.map((app, i) => (
          <div key={app.name} className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-muted-foreground truncate">
              <span
                className="size-2 rounded-full shrink-0"
                style={{ backgroundColor: COLORS[i % COLORS.length] }}
              />
              <span className="truncate">{app.name}</span>
            </span>
            <span className="font-mono text-foreground shrink-0 ml-2">
              {formatMinutes(app.minutes)}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
