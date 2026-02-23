/**
 * StatCard
 *
 * Props:
 *   name        {string}         — label shown below the primary value
 *   value       {string|number}  — main figure (bold black)
 *   extra       {string|number}  [optional] — secondary note (right side)
 *   percentage  {number}         [optional] — 0-100, drives the progress bar fill
 *   barColor    {string}         [optional] — Tailwind bg class e.g. 'bg-green-500'
 */
export default function StatCard({ name, value, extra, percentage, barColor = 'bg-gray-400' }) {
  const pct = percentage !== undefined ? Math.min(100, Math.max(0, percentage)) : null

  return (
    <div className="bg-white border border-gray-200 rounded-[5px] p-5 flex flex-col gap-[10px] min-w-[190px]">
      {/* Title */}
      <span className="text-xs font-medium text-gray-500 uppercase tracking-wide">
        {name}
      </span>

      {/* Value row */}
      <div className="flex flex-row items-start justify-between gap-[10px]">
        <span className="text-3xl font-bold text-black leading-none">
          {value}
        </span>
        {extra !== undefined && extra !== null && extra !== '' && (
          <span className="text-xs text-gray-400 text-right min-w-[60px] self-end">
            {extra}
          </span>
        )}
      </div>

      {/* Progress bar — only shown when percentage is provided */}
      {pct !== null && (
        <div className="w-full h-1.5 bg-gray-100 rounded-full overflow-hidden">
          <div
            className={`h-full rounded-full ${barColor}`}
            style={{ width: `${pct}%` }}
          />
        </div>
      )}
    </div>
  )
}
