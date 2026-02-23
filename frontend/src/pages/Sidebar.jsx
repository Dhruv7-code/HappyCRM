import { NavLink } from 'react-router-dom'
import worldImg     from '../assets/world.png'
import dashboardImg from '../assets/dashboard.png'
import usersImg     from '../assets/users.png'
import jobsImg      from '../assets/jobs.png'
import executionImg from '../assets/execution.png'

const navItems = [
  { to: '/dashboard',        label: 'Dashboard',        icon: dashboardImg },
  { to: '/customers',        label: 'Customers',        icon: usersImg     },
  { to: '/integration-jobs', label: 'Integration Jobs', icon: jobsImg      },
  { to: '/execution',        label: 'Execution',        icon: executionImg },
]

export default function Sidebar() {
  return (
    <div className="flex flex-col h-full">

      {/* ── Top 10% — brand header ── */}
      <div className="h-[10%] flex items-center justify-center border-b-2 border-white shrink-0">
        <div className="flex items-center gap-2.5">
          <img src={worldImg} alt="Happy CRM logo" className="w-7 h-7 object-contain" />
          <span className="text-white font-semibold text-sm tracking-wide">Happy CRM</span>
        </div>
      </div>

      {/* ── Bottom 90% — navigation ── */}
      <nav className="flex-1 flex flex-col pt-5 gap-2.5">
        {navItems.map(({ to, label, icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              `w-full flex flex-row items-center pl-22 gap-2.5 py-3 text-xs font-medium tracking-wide transition-colors
               ${isActive
                 ? 'bg-gray-700 text-white'
                 : 'text-gray-400 hover:bg-gray-700 hover:text-white'}`
            }
          >
            <img src={icon} alt={label} className="w-5 h-5 object-contain shrink-0" />
            <span>{label}</span>
          </NavLink>
        ))}
      </nav>

    </div>
  )
}
