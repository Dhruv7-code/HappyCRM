import { useState, useEffect } from 'react'
import IntegrationJobsTable from '../components/IntegrationJobsTable'
import { getIntegrationJobs } from '../api/jobsApi'

export default function IntegrationJobs() {
  const [jobs, setJobs] = useState([])

  useEffect(() => {
    getIntegrationJobs()
      .then(data => setJobs(data))
      .catch(() => setJobs([]))
  }, [])

  return (
    <div className="flex flex-col h-full bg-gray-300">

      {/* Page title */}
      <div className="px-8 py-8 border-b-2 border-white bg-gray-300 flex flex-row items-center justify-between">
        <h1 className="text-xl font-bold text-gray-800">Integration Jobs</h1>
      </div>

      {/* Divider */}
      <div className="border-t border-white" />

      {/* Content */}
      <div className="px-8 py-6">
        <IntegrationJobsTable rows={jobs} />
      </div>

    </div>
  )
}
