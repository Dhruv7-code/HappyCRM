/**
 * RatesCard
 *
 * Shows up to 3 horizontal progress bars in 3 equal columns divided by vertical lines.
 *
 * Props:
 *   sections  {Array<{ status: string, percentage: number, color: string }>}
 *             — exactly 3 items expected
 *             — color is a Tailwind bg class e.g. 'bg-green-500'
 */
export default function RatesCard({ sections = [] }) {
  return (
    <div className="bg-white rounded-[5px] border border-gray-200 overflow-hidden w-full">

      {/* 3 equal columns divided by vertical lines */}
      <div className="flex divide-x divide-gray-200">
        {sections.map(({ status, percentage, color }, i) => {
          const pct = Math.min(100, Math.max(0, percentage))
          return (
            <div key={i} className="flex-1 px-5 py-4 flex flex-col gap-3">

              {/* Status left — percentage right */}
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-gray-600">{status}</span>
                <span className="text-xs font-semibold text-gray-800">{pct}%</span>
              </div>

              {/* Progress bar */}
              <div className="w-full h-2 bg-gray-100 rounded-full overflow-hidden">
                <div
                  className={`h-full rounded-full ${color}`}
                  style={{ width: `${pct}%` }}
                />
              </div>

            </div>
          )
        })}
      </div>

    </div>
  )
}
