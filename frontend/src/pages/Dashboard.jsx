import { useState, useEffect } from 'react'
import StatCard from '../components/StatCard'
import RatesCard from '../components/RatesCard'
import RecentJobsTable from '../components/RecentJobsTable'
import { getTotalCustomers } from '../api/customersApi'
import { getSuccessStats, getFailedStats, getPendingStats } from '../api/statsApi'

export default function Dashboard() {
  const [totalCustomers, setTotalCustomers] = useState('—')
  const [success, setSuccess] = useState({ count: '—', percentage: 0 })
  const [failed,  setFailed]  = useState({ count: '—', percentage: 0 })
  const [pending, setPending] = useState({ count: '—', percentage: 0 })

  useEffect(() => {
    getTotalCustomers()
      .then(count => setTotalCustomers(count))
      .catch(() => setTotalCustomers('—'))

    getSuccessStats()
      .then(data => setSuccess(data))
      .catch(() => setSuccess({ count: '—', percentage: 0 }))

    getFailedStats()
      .then(data => setFailed(data))
      .catch(() => setFailed({ count: '—', percentage: 0 }))

    getPendingStats()
      .then(data => setPending(data))
      .catch(() => setPending({ count: '—', percentage: 0 }))
  }, [])

  return (
    <div className="flex flex-col h-full bg-gray-300">
      {/* Page title */}
      <div className="px-8 py-8 border-b-2 border-white bg-gray-300">
        <h1 className="text-xl font-bold text-gray-800">Dashboard</h1>
      </div>

      {/* Divider */}
      <div className="border-t border-white" />

      {/* Content — all sections share the same column width */}
      <div className="px-8 py-6 flex flex-col gap-15">

        {/* Shared-width column */}
        <div className="flex flex-col gap-2.5 w-fit">

          {/* Stat cards row */}
          <div className="flex flex-row gap-17.5">
            <StatCard name="Total Customers" value={totalCustomers} />
            <StatCard
              name="Success"
              value={`${success.percentage}%`}
              percentage={success.percentage}
              barColor="bg-green-500"
            />
            <StatCard
              name="Failed"
              value={`${failed.percentage}%`}
              percentage={failed.percentage}
              barColor="bg-red-500"
            />
            <StatCard
              name="Pending"
              value={`${pending.percentage}%`}
              percentage={pending.percentage}
              barColor="bg-yellow-400"
            />
          </div>

          {/* Rates card — full width of the stat cards row */}
          <div className="w-full">
            <RatesCard
              sections={[
                { status: 'Success', percentage: success.percentage, color: 'bg-green-500' },
                { status: 'Failed',  percentage: failed.percentage,  color: 'bg-red-500'   },
                { status: 'Pending', percentage: pending.percentage,  color: 'bg-yellow-400' },
              ]}
            />
          </div>

          {/* Recent Job Executions */}
          <div className="flex flex-col w-full" style={{ gap: '20px', marginTop: '10px' }}>
            <h4 className="text-sm font-semibold text-gray-800">Recent Job Executions</h4>
            <RecentJobsTable />
          </div>

        </div>
      </div>
    </div>
  )
}
