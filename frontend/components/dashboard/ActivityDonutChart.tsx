"use client";

import { Cell, Pie, PieChart, ResponsiveContainer } from "recharts";

type ActivityDonutChartProps = {
  active: number;
  absent: number;
  inactive: number;
};

const COLORS = {
  Activo: "#6EC531",
  Ausente: "#f59e0b",
  Inactivo: "#71717a",
};

export function ActivityDonutChart({
  active,
  absent,
  inactive,
}: ActivityDonutChartProps) {
  const total = active + absent + inactive;
  const data = [
    { name: "Activo", value: active },
    { name: "Ausente", value: absent },
    { name: "Inactivo", value: inactive },
  ];
  const activePct = total > 0 ? Math.round((active / total) * 100) : 0;

  return (
    <div className="rounded-[var(--radius)] border border-border bg-card p-5 flex flex-col">
      <h3 className="text-sm font-medium text-foreground">Actividad global</h3>
      <p className="text-xs text-muted-foreground mt-0.5 mb-3">
        Distribución de estados actual
      </p>

      <div className="relative h-44">
        {total === 0 ? (
          <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
            Sin datos
          </div>
        ) : (
          <>
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={data}
                  innerRadius={50}
                  outerRadius={75}
                  paddingAngle={2}
                  dataKey="value"
                  stroke="none"
                >
                  {data.map((entry) => (
                    <Cell
                      key={entry.name}
                      fill={COLORS[entry.name as keyof typeof COLORS]}
                    />
                  ))}
                </Pie>
              </PieChart>
            </ResponsiveContainer>
            <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
              <span className="text-2xl font-mono text-foreground">
                {activePct}%
              </span>
              <span className="text-xs text-muted-foreground">Activos</span>
            </div>
          </>
        )}
      </div>

      <div className="flex flex-col gap-1.5 mt-4 text-xs">
        {data.map((d) => (
          <div key={d.name} className="flex items-center justify-between">
            <span className="flex items-center gap-2 text-muted-foreground">
              <span
                className="size-2 rounded-full"
                style={{ backgroundColor: COLORS[d.name as keyof typeof COLORS] }}
              />
              {d.name}
            </span>
            <span className="font-mono text-foreground">{d.value}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
