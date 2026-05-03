"use client";

import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

export type HourlyDataPoint = {
  hour: number;
  activeWorkers: number;
  productiveMinutes: number;
};

type HourlyActivityChartProps = {
  data: HourlyDataPoint[];
};

function formatHour(h: number): string {
  return `${h.toString().padStart(2, "0")}h`;
}

export function HourlyActivityChart({ data }: HourlyActivityChartProps) {
  const total = data.reduce((acc, d) => acc + d.productiveMinutes, 0);

  return (
    <div className="rounded-[var(--radius)] border border-border bg-card p-5 flex flex-col">
      <div className="flex items-baseline justify-between mb-1">
        <h3 className="text-sm font-medium text-foreground">
          Productividad por hora
        </h3>
        <span className="text-xs text-muted-foreground font-mono">
          {total} min totales · Madrid
        </span>
      </div>
      <p className="text-xs text-muted-foreground mt-0.5 mb-3">
        Minutos productivos acumulados por hora hoy
      </p>

      <div className="h-56">
        {total === 0 ? (
          <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
            Sin actividad registrada hoy
          </div>
        ) : (
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart
              data={data}
              margin={{ top: 8, right: 8, left: -20, bottom: 0 }}
            >
              <defs>
                <linearGradient id="hourlyFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#6EC531" stopOpacity={0.4} />
                  <stop offset="100%" stopColor="#6EC531" stopOpacity={0.02} />
                </linearGradient>
              </defs>
              <CartesianGrid
                strokeDasharray="3 3"
                vertical={false}
                stroke="var(--border)"
              />
              <XAxis
                dataKey="hour"
                tick={{ fill: "var(--muted-foreground)", fontSize: 11 }}
                tickFormatter={formatHour}
                axisLine={{ stroke: "var(--border)" }}
                tickLine={false}
              />
              <YAxis
                tick={{ fill: "var(--muted-foreground)", fontSize: 11 }}
                axisLine={{ stroke: "var(--border)" }}
                tickLine={false}
                allowDecimals={false}
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "var(--card)",
                  border: "1px solid var(--border)",
                  borderRadius: "0.5rem",
                  fontSize: "0.75rem",
                  color: "var(--foreground)",
                }}
                labelFormatter={(label) => `${formatHour(Number(label) || 0)} Madrid`}
                formatter={(value, key) => {
                  const k = String(key);
                  if (k === "productiveMinutes")
                    return [`${value} min`, "Productivos"];
                  if (k === "activeWorkers")
                    return [String(value), "Workers activos"];
                  return [String(value), k];
                }}
              />
              <Area
                type="monotone"
                dataKey="productiveMinutes"
                stroke="#6EC531"
                strokeWidth={2}
                fill="url(#hourlyFill)"
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  );
}
