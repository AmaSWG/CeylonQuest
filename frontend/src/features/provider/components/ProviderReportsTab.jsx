import { useState } from 'react'
import { BarChartIcon } from '../../../components/Icons'
import InventoryReportView from '../../../components/InventoryReportView'
import ProviderBookingsRevenueReportTab from './ProviderBookingsRevenueReportTab'

export default function ProviderReportsTab({ token, onLogout }) {
  const [reportSubTab, setReportSubTab] = useState('inventory')

  return (
    <div className="ad-report">
      <div className="cq-report-subtabs">
        <button
          type="button"
          className={`cq-report-subtab-btn ${reportSubTab === 'inventory' ? 'active' : ''}`}
          onClick={() => setReportSubTab('inventory')}
        >
          <BarChartIcon size={16} /> Inventory Report
        </button>
        <button
          type="button"
          className={`cq-report-subtab-btn ${reportSubTab === 'bookings' ? 'active' : ''}`}
          onClick={() => setReportSubTab('bookings')}
        >
          <BarChartIcon size={16} /> Bookings &amp; Revenue Report
        </button>
      </div>

      {reportSubTab === 'inventory' ? (
        <InventoryReportView token={token} onLogout={onLogout} isAdmin={false} />
      ) : (
        <ProviderBookingsRevenueReportTab token={token} onLogout={onLogout} />
      )}
    </div>
  )
}
