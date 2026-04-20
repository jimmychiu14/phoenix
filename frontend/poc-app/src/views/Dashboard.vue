<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue'
import { PlusIcon, XMarkIcon, TrashIcon, ArrowPathIcon, PencilIcon, ChartBarIcon, ArrowDownTrayIcon, SunIcon, MoonIcon } from '@heroicons/vue/24/outline'
import { Doughnut, Line } from 'vue-chartjs'
import { 
  Chart as ChartJS, 
  ArcElement, 
  Tooltip, 
  Legend, 
  CategoryScale, 
  LinearScale, 
  PointElement, 
  LineElement,
  Title
} from 'chart.js'
import { useAuthStore } from '../stores/auth'
import api from '../api'

ChartJS.register(
  ArcElement, 
  Tooltip, 
  Legend, 
  CategoryScale, 
  LinearScale, 
  PointElement, 
  LineElement,
  Title
)

interface Asset {
  id: number
  symbol: string
  name: string
  price: number
  quantity: number
  lastUpdated: string
}

const assets = ref<Asset[]>([])
const error = ref<string | null>(null)
const loading = ref(true)
const authStore = useAuthStore()
const refreshing = ref(false)
const refreshStatus = ref<Record<number, 'ok' | 'err' | null>>({})

// Dark Mode State
const isDark = ref(localStorage.getItem('theme') === 'dark')

const toggleTheme = () => {
  isDark.value = !isDark.value
  localStorage.setItem('theme', isDark.value ? 'dark' : 'light')
  updateThemeClass()
}

const updateThemeClass = () => {
  if (isDark.value) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

const REFRESH_INTERVAL = 300 // 5 minutes in seconds
const autoRefreshEnabled = ref(true)
const countdown = ref(REFRESH_INTERVAL)
let timerInterval: number | null = null

const startTimer = () => {
  stopTimer()
  if (autoRefreshEnabled.value) {
    timerInterval = window.setInterval(async () => {
      if (countdown.value > 0) {
        countdown.value--
      } else {
        await refreshAllPrices()
        countdown.value = REFRESH_INTERVAL
      }
    }, 1000)
  }
}

const stopTimer = () => {
  if (timerInterval) {
    window.clearInterval(timerInterval)
    timerInterval = null
  }
}

const toggleAutoRefresh = () => {
  if (autoRefreshEnabled.value) {
    countdown.value = REFRESH_INTERVAL
    startTimer()
  } else {
    stopTimer()
  }
}

const refreshAllPrices = async () => {
  refreshing.value = true
  refreshStatus.value = {}
  await Promise.allSettled(
    assets.value.map(async (asset) => {
      try {
        const res = await fetch(`https://phoenix-17zh.onrender.com/api/quote/${asset.symbol}`)
        if (!res.ok) throw new Error('No data')
        const data = await res.json()
        const updated = await api.patch(`/assets/${asset.id}/price`, { price: data.price })
        // Update local state instantly
        asset.price = data.price
        asset.lastUpdated = updated.data.lastUpdated
        refreshStatus.value[asset.id] = 'ok'
      } catch {
        refreshStatus.value[asset.id] = 'err'
      }
    })
  )
  refreshing.value = false
}

const isAddModalOpen = ref(false)
const submitting = ref(false)
const fetchingQuote = ref(false)
const addError = ref<string | null>(null)
const newAsset = ref({
  symbol: '',
  name: '',
  price: 0,
  quantity: 0
})

// Edit Modal state
const isEditModalOpen = ref(false)
const editSubmitting = ref(false)
const editError = ref<string | null>(null)
const editingAsset = ref<Asset | null>(null)
const editForm = ref({ symbol: '', name: '', price: 0, quantity: 0 })

// History Modal state
const isHistoryModalOpen = ref(false)
const historyLoading = ref(false)
const historySymbol = ref('')
const historyData = ref<{ date: string, price: number }[]>([])

const openEditModal = (asset: Asset) => {
  editingAsset.value = asset
  editForm.value = {
    symbol: asset.symbol,
    name: asset.name,
    price: asset.price,
    quantity: asset.quantity
  }
  editError.value = null
  isEditModalOpen.value = true
}

const closeEditModal = () => {
  isEditModalOpen.value = false
  editingAsset.value = null
}

const submitEdit = async () => {
  if (!editingAsset.value) return
  try {
    editSubmitting.value = true
    editError.value = null
    const response = await api.put(`/assets/${editingAsset.value.id}`, {
      symbol: editForm.value.symbol.toUpperCase(),
      name: editForm.value.name,
      price: Number(editForm.value.price),
      quantity: Number(editForm.value.quantity)
    })
    // Update local state
    const idx = assets.value.findIndex(a => a.id === editingAsset.value!.id)
    if (idx !== -1) assets.value[idx] = response.data
    closeEditModal()
  } catch (e: any) {
    editError.value = e?.response?.data?.message || 'Failed to update asset.'
  } finally {
    editSubmitting.value = false
  }
}

const openHistoryModal = async (asset: Asset) => {
  historySymbol.value = asset.symbol
  isHistoryModalOpen.value = true
  historyLoading.value = true
  historyData.value = []
  
  try {
    const response = await fetch(`https://phoenix-17zh.onrender.com/api/history/${asset.symbol}`)
    if (!response.ok) throw new Error('Failed to fetch history')
    historyData.value = await response.json()
  } catch (err) {
    console.error('Error fetching history:', err)
  } finally {
    historyLoading.value = false
  }
}

const closeHistoryModal = () => {
  isHistoryModalOpen.value = false
}

const lineChartData = computed(() => {
  const labels = historyData.value.map(d => d.date.split('-').slice(1).join('/'))
  const datasets = [
    {
      label: `${historySymbol.value} Price`,
      data: historyData.value.map(d => d.price),
      borderColor: isDark.value ? '#22d3ee' : '#6366f1',
      backgroundColor: isDark.value ? 'rgba(34, 211, 238, 0.1)' : 'rgba(99, 102, 241, 0.1)',
      borderWidth: 2.5,
      fill: true,
      tension: 0.4,
      pointRadius: 1,
      pointHoverRadius: 6
    }
  ]

  // Add SMA20 if available
  if (historyData.value.some(d => (d as any).sma20)) {
    datasets.push({
      label: 'SMA 20',
      data: historyData.value.map(d => (d as any).sma20 || null),
      borderColor: '#f59e0b', // amber-500
      backgroundColor: 'transparent',
      borderWidth: 1.5,
      fill: false,
      tension: 0.4,
      pointRadius: 0,
      pointHoverRadius: 0
    })
  }

  // Add SMA50 if available
  if (historyData.value.some(d => (d as any).sma50)) {
    datasets.push({
      label: 'SMA 50',
      data: historyData.value.map(d => (d as any).sma50 || null),
      borderColor: '#ef4444', // red-500
      backgroundColor: 'transparent',
      borderWidth: 1.5,
      fill: false,
      tension: 0.4,
      pointRadius: 0,
      pointHoverRadius: 0
    })
  }

  return { labels, datasets }
})

const lineChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { 
      display: true,
      position: 'top' as const,
      labels: {
        color: isDark.value ? '#cbd5e1' : '#475569',
        font: { size: 10 },
        boxWidth: 20,
        usePointStyle: true
      }
    },
    tooltip: {
      mode: 'index' as const,
      intersect: false,
      backgroundColor: isDark.value ? '#1e293b' : '#ffffff',
      titleColor: isDark.value ? '#f8fafc' : '#1e293b',
      bodyColor: isDark.value ? '#cbd5e1' : '#475569',
      borderColor: isDark.value ? '#334155' : '#e2e8f0',
      borderWidth: 1,
      callbacks: {
        label: (ctx: any) => ` $${ctx.parsed.y.toFixed(2)}`
      }
    }
  },
  scales: {
    y: {
      beginAtZero: false,
      grid: { color: isDark.value ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.05)' },
      ticks: { 
        color: isDark.value ? '#94a3b8' : '#64748b',
        font: { size: 11 }
      }
    },
    x: {
      grid: { display: false },
      ticks: { 
        color: isDark.value ? '#94a3b8' : '#64748b',
        font: { size: 11 }
      }
    }
  }
}))

const exportToCSV = () => {
  if (assets.value.length === 0) return

  const headers = ['Symbol', 'Name', 'Shares', 'Current Price', 'Position Value', 'Last Sync']
  const rows = assets.value.map(asset => [
    asset.symbol,
    `"${asset.name.replace(/"/g, '""')}"`, // Escape quotes for CSV
    asset.quantity,
    asset.price.toFixed(2),
    (asset.price * asset.quantity).toFixed(2),
    new Date(asset.lastUpdated).toLocaleString()
  ])

  const csvContent = [
    headers.join(','),
    ...rows.map(row => row.join(','))
  ].join('\n')

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  const dateStr = new Date().toISOString().split('T')[0]
  
  link.setAttribute('href', url)
  link.setAttribute('download', `portfolio_export_${dateStr}.csv`)
  link.style.visibility = 'hidden'
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
}

const fetchLiveQuote = async () => {
  if (!newAsset.value.symbol) return;
  fetchingQuote.value = true;
  addError.value = null;
  try {
    const symbol = newAsset.value.symbol.toUpperCase();
    const response = await fetch(`https://phoenix-17zh.onrender.com/api/quote/${symbol}`);
    if (!response.ok) {
       throw new Error('Quote not found.');
    }
    const data = await response.json();
    newAsset.value.name = data.name;
    newAsset.value.price = data.price;
  } catch (err) {
    console.warn("Could not fetch live true price", err);
  } finally {
    fetchingQuote.value = false;
  }
}

const openAddModal = () => {
  addError.value = null
  newAsset.value = { symbol: '', name: '', price: 0, quantity: 0 }
  isAddModalOpen.value = true
}

const closeAddModal = () => {
  isAddModalOpen.value = false
}

const submitAsset = async () => {
  try {
    submitting.value = true
    addError.value = null
    const response = await api.post('/assets', {
      symbol: newAsset.value.symbol.toUpperCase(),
      name: newAsset.value.name,
      price: Number(newAsset.value.price),
      quantity: Number(newAsset.value.quantity)
    })
    assets.value.push(response.data)
    closeAddModal()
  } catch (e: any) {
    if (e.response && e.response.data) {
       addError.value = e.response.data.message || 'Failed to add asset.'
    } else {
       addError.value = e.message || 'Failed to connect.'
    }
  } finally {
    submitting.value = false
  }
}

const deleteAsset = async (id: number, name: string) => {
  if (confirm(`Are you sure you want to delete ${name}?`)) {
    try {
      await api.delete(`/assets/${id}`)
      assets.value = assets.value.filter(a => a.id !== id)
    } catch (e: any) {
      alert(e?.response?.data?.message || 'Failed to delete asset.')
    }
  }
}

const totalValue = computed(() => {
  return assets.value.reduce((sum, item) => sum + (item.price * item.quantity), 0)
})

const CHART_COLORS = [
  '#6366f1', '#8b5cf6', '#ec4899', '#f59e0b', '#10b981',
  '#3b82f6', '#ef4444', '#14b8a6', '#f97316', '#a855f7'
]

const chartData = computed(() => {
  const withValue = assets.value.filter(a => a.price * a.quantity > 0)
  return {
    labels: withValue.map(a => a.symbol),
    datasets: [{
      data: withValue.map(a => +(a.price * a.quantity).toFixed(2)),
      backgroundColor: withValue.map((_, i) => CHART_COLORS[i % CHART_COLORS.length]),
      borderWidth: 2,
      borderColor: isDark.value ? '#1e293b' : '#ffffff',
      hoverOffset: 8
    }]
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'right' as const,
      labels: {
        usePointStyle: true,
        padding: 16,
        color: isDark.value ? '#cbd5e1' : '#475569', // slate-300 in dark, slate-600 in light
        font: { size: 13, family: 'Inter, sans-serif' }
      }
    },
    tooltip: {
      callbacks: {
        label: (ctx: any) => {
          const val = ctx.parsed
          const total = ctx.dataset.data.reduce((a: number, b: number) => a + b, 0)
          const pct = ((val / total) * 100).toFixed(1)
          return ` $${val.toLocaleString()} (${pct}%)`
        }
      }
    }
  }
}))

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString('zh-TW', {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const handleLogout = () => {
  authStore.logout()
}

onMounted(async () => {
  // Initialize theme from localStorage
  isDark.value = localStorage.getItem('theme') === 'dark'
  updateThemeClass()

  loading.value = true
  error.value = null
  try {
    const response = await api.get('/assets')
    assets.value = response.data
  } catch (e: any) {
    if (e.response && e.response.status === 401) {
       error.value = 'Session expired. Please log in again.'
       authStore.logout()
    } else {
       error.value = e.message || 'Failed to fetch assets.'
    }
  } finally {
    loading.value = false
    startTimer()
  }
})

onUnmounted(() => {
  stopTimer()
})
</script>

<template>
  <div class="min-h-screen bg-gradient-to-br from-slate-50 to-slate-100 dark:from-slate-950 dark:to-slate-900 py-12 px-4 sm:px-6 lg:px-8 transition-colors duration-500">
    <div class="max-w-7xl mx-auto space-y-8">
      
      <!-- Header Section -->
      <header class="flex flex-col md:flex-row md:items-center md:justify-between space-y-4 md:space-y-0">
        <div class="space-y-1">
          <h1 class="text-4xl font-extrabold tracking-tight text-slate-900 dark:text-white flex items-center">
            Phoenix Portfolio
            <div class="ml-3 h-2 w-2 bg-indigo-500 rounded-full animate-pulse"></div>
          </h1>
          <p class="text-slate-500 dark:text-slate-400 font-medium">Manage your quantitative trading assets and positions.</p>
        </div>
        
        <div class="flex flex-wrap items-center gap-3">
          <div class="bg-white dark:bg-slate-800 px-5 py-3 rounded-xl shadow-sm border border-slate-200 dark:border-slate-700 flex items-center space-x-4 hidden sm:flex">
            <span class="text-sm font-medium text-slate-500 dark:text-slate-400">Total Value</span>
            <span class="text-2xl font-bold text-indigo-600 dark:text-indigo-400">${{ totalValue.toLocaleString(undefined, { minimumFractionDigits: 2 }) }}</span>
          </div>

          <!-- Auto Refresh Control -->
          <div class="flex items-center space-x-2 bg-white dark:bg-slate-800 px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 shadow-sm">
            <div class="flex items-center">
              <input 
                id="auto-refresh" 
                v-model="autoRefreshEnabled" 
                type="checkbox" 
                @change="toggleAutoRefresh"
                class="h-4 w-4 text-indigo-600 focus:ring-indigo-500 border-slate-300 dark:border-slate-600 dark:bg-slate-900 rounded cursor-pointer"
              />
              <label for="auto-refresh" class="ml-2 text-xs font-semibold text-slate-700 dark:text-slate-300 cursor-pointer select-none">Auto</label>
            </div>
            <div v-if="autoRefreshEnabled" class="text-[10px] font-mono font-bold text-indigo-500 dark:text-indigo-400 tabular-nums bg-indigo-50 dark:bg-indigo-950/30 px-1.5 py-0.5 rounded border border-indigo-100 dark:border-indigo-900 min-w-[35px] text-center">
              {{ Math.floor(countdown / 60) }}:{{ (countdown % 60).toString().padStart(2, '0') }}
            </div>
          </div>

          <button @click="exportToCSV" :disabled="assets.length === 0" title="Export portfolio to CSV" class="flex items-center px-4 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-semibold rounded-lg shadow-sm hover:bg-slate-200 dark:hover:bg-slate-700 disabled:opacity-50 transition-colors border border-transparent dark:border-slate-700">
            <ArrowDownTrayIcon class="h-5 w-5 sm:mr-1" />
            <span class="hidden sm:inline">Export</span>
          </button>

          <button @click="refreshAllPrices" :disabled="refreshing" title="Refresh all prices from market data" class="flex items-center px-4 py-2.5 bg-emerald-600 text-white font-semibold rounded-lg shadow-sm hover:bg-emerald-700 disabled:opacity-50 transition-colors">
            <ArrowPathIcon :class="['h-5 w-5 sm:mr-1', refreshing ? 'animate-spin' : '']" />
            <span class="hidden sm:inline">{{ refreshing ? 'Refreshing...' : 'Refresh Prices' }}</span>
          </button>
          
          <button @click="openAddModal" class="flex items-center px-4 py-2.5 bg-indigo-600 text-white font-semibold rounded-lg shadow-sm hover:bg-indigo-700 transition-colors">
            <PlusIcon class="h-5 w-5 sm:mr-1" />
            <span class="hidden sm:inline">Add Asset</span>
          </button>

          <button @click="toggleTheme" class="p-2.5 bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-lg border border-slate-200 dark:border-slate-700 shadow-sm hover:bg-slate-50 dark:hover:bg-slate-700 transition-all" :title="isDark ? 'Switch to Light Mode' : 'Switch to Dark Mode'">
            <SunIcon v-if="isDark" class="h-5 w-5" />
            <MoonIcon v-else class="h-5 w-5" />
          </button>
          
          <button @click="handleLogout" class="px-5 py-2.5 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-semibold rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors border border-transparent dark:border-slate-700">
            Logout
          </button>
        </div>
      </header>

      <!-- Stats & Main Chart Card -->
      <div class="bg-white dark:bg-slate-800 rounded-3xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-100 dark:border-slate-700 overflow-hidden transition-all duration-300">
        <div class="px-8 py-6 border-b border-slate-50 dark:border-slate-700/50 flex items-center justify-between bg-slate-50/50 dark:bg-slate-800/50">
          <h2 class="text-xl font-bold text-slate-900 dark:text-white flex items-center">
            Portfolio Allocation
          </h2>
          <div v-if="assets.length > 0" class="text-sm font-medium text-slate-400 dark:text-slate-500">
            {{ assets.length }} Positions Tracked
          </div>
        </div>
        <div class="p-8">
          <div v-if="loading" class="h-64 flex flex-col items-center justify-center space-y-4">
            <ArrowPathIcon class="h-10 w-10 text-indigo-500 animate-spin" />
            <p class="text-slate-500 dark:text-slate-400 font-medium">Loading your portfolio...</p>
          </div>
          <div v-else-if="assets.length > 0" class="h-64 relative">
             <Doughnut :data="chartData" :options="chartOptions" />
          </div>
          <div v-else class="h-64 flex flex-col items-center justify-center text-slate-400 dark:text-slate-500 text-center space-y-2">
             <p class="text-lg font-medium">Your portfolio is empty.</p>
             <p class="text-sm">Add your first asset to see the allocation breakdown.</p>
          </div>
        </div>
      </div>

      <!-- Asset Table Section -->
      <div class="bg-white dark:bg-slate-800 rounded-3xl shadow-xl shadow-slate-200/50 dark:shadow-none border border-slate-100 dark:border-slate-700 overflow-hidden transition-all duration-300">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-slate-100 dark:divide-slate-700">
            <thead class="bg-slate-50/50 dark:bg-slate-900/80">
              <tr>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest">Asset</th>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest">Symbol</th>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest text-right">Shares</th>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest text-right">Price</th>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest text-right">Value</th>
                <th scope="col" class="px-6 py-4 text-left text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest">Sync</th>
                <th scope="col" class="px-6 py-4 text-right text-xs font-bold text-slate-400 dark:text-slate-400 uppercase tracking-widest">Action</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-50 dark:divide-slate-700/50">
              <tr v-for="asset in assets" :key="asset.id" class="hover:bg-slate-50/80 dark:hover:bg-slate-700/30 transition-colors group">
                <td class="px-6 py-5 whitespace-nowrap">
                  <div class="flex items-center">
                    <div class="h-10 w-10 flex-shrink-0 flex items-center justify-center bg-indigo-50 dark:bg-indigo-950/50 text-indigo-600 dark:text-indigo-400 rounded-xl font-bold text-sm">
                      {{ asset.symbol.charAt(0) }}
                    </div>
                    <div class="ml-4">
                      <div class="text-sm font-bold text-slate-900 dark:text-white">{{ asset.name }}</div>
                      <div class="text-xs font-medium text-slate-400 dark:text-slate-500">ID: {{ asset.id }}</div>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-5 whitespace-nowrap">
                  <span class="px-3 py-1 bg-slate-100 dark:bg-slate-700 text-slate-600 dark:text-slate-300 rounded-lg text-xs font-bold tracking-wider">
                    {{ asset.symbol }}
                  </span>
                </td>
                <td class="px-6 py-5 whitespace-nowrap text-sm font-semibold text-right text-slate-700 dark:text-slate-300">
                  {{ asset.quantity }}
                </td>
                <td class="px-6 py-5 whitespace-nowrap text-sm font-bold text-right text-slate-900 dark:text-white">
                  ${{ asset.price.toLocaleString(undefined, { minimumFractionDigits: 2 }) }}
                </td>
                <td class="px-6 py-5 whitespace-nowrap text-sm font-bold text-right text-indigo-600 dark:text-indigo-400">
                  ${{ (asset.price * asset.quantity).toLocaleString(undefined, { minimumFractionDigits: 2 }) }}
                </td>
                <td class="px-6 py-5 whitespace-nowrap text-xs font-medium text-slate-400 dark:text-slate-500">
                  {{ formatDate(asset.lastUpdated) }}
                </td>
                <td class="px-6 py-5 whitespace-nowrap text-right text-sm font-medium">
                  <div class="flex items-center justify-end space-x-1">
                    <button @click="openHistoryModal(asset)" class="text-emerald-500 dark:text-emerald-400 hover:text-emerald-700 dark:hover:text-emerald-300 transition-colors p-2 rounded-lg hover:bg-emerald-50 dark:hover:bg-emerald-950/50 focus:outline-none" title="View Chart">
                      <ChartBarIcon class="h-5 w-5 inline-block" />
                    </button>
                    <button @click="openEditModal(asset)" class="text-indigo-400 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 transition-colors p-2 rounded-lg hover:bg-indigo-50 dark:hover:bg-indigo-950/50 focus:outline-none" title="Edit Asset">
                      <PencilIcon class="h-5 w-5 inline-block" />
                    </button>
                    <button @click="deleteAsset(asset.id, asset.name)" class="text-rose-400 hover:text-rose-700 dark:hover:text-rose-300 transition-colors p-2 rounded-lg hover:bg-rose-50 dark:hover:bg-rose-950/50 focus:outline-none" title="Delete Asset">
                      <TrashIcon class="h-5 w-5 inline-block" />
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- History Chart Modal Overlay -->
    <div v-if="isHistoryModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 dark:bg-black/70 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden flex flex-col transform transition-all border border-transparent dark:border-slate-800">
        <!-- Modal Header -->
        <div class="px-6 py-4 bg-slate-50 dark:bg-slate-900/50 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <h3 class="text-xl font-bold text-slate-900 dark:text-white">
            <span class="text-slate-400 dark:text-slate-500 font-normal">{{ historySymbol }}</span> Performance
          </h3>
          <button @click="closeHistoryModal" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 p-2 rounded-full hover:bg-slate-200 dark:hover:bg-slate-800 transition-all">
            <XMarkIcon class="h-6 w-6" />
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-6">
          <div v-if="historyLoading" class="h-64 flex flex-col items-center justify-center space-y-4">
            <ArrowPathIcon class="h-10 w-10 text-emerald-500 animate-spin" />
            <p class="text-slate-500 dark:text-slate-400 font-medium">Fetching historical data...</p>
          </div>
          <div v-else-if="historyData.length > 0" class="h-80">
            <Line :data="lineChartData" :options="lineChartOptions" />
          </div>
          <div v-else class="h-64 flex flex-center text-slate-400 dark:text-slate-500">
            No historical data available.
          </div>
          
          <div class="mt-6 flex justify-end">
            <button
              @click="closeHistoryModal"
              class="px-6 py-2 bg-slate-100 dark:bg-slate-700 text-slate-700 dark:text-slate-300 font-semibold rounded-lg hover:bg-slate-200 dark:hover:bg-slate-600 transition-colors"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Asset Modal Overlay -->
    <div v-if="isAddModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 dark:bg-black/80 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col transform transition-all border border-transparent dark:border-slate-800">
        <div class="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50">
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">Add New Asset</h3>
          <button @click="isAddModalOpen = false" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors rounded-full p-1 hover:bg-slate-200 dark:hover:bg-slate-700">
            <XMarkIcon class="h-6 w-6" />
          </button>
        </div>
        <form @submit.prevent="submitAsset" class="p-6 space-y-5">
          <div v-if="addError" class="bg-red-50 dark:bg-red-950/30 text-red-600 dark:text-red-400 text-sm p-3 rounded-lg border border-red-100 dark:border-red-900/50 flex items-start">
            <span class="mr-2">⚠️</span>
            <span>{{ addError }}</span>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Symbol</label>
            <div class="relative">
              <input v-model="newAsset.symbol" @blur="fetchLiveQuote" type="text" required placeholder="e.g. MSFT" class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all uppercase placeholder:normal-case" />
              <div v-if="fetchingQuote" class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
                <ArrowPathIcon class="h-5 w-5 text-indigo-500 animate-spin" />
              </div>
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Company Name</label>
            <input v-model="newAsset.name" type="text" required placeholder="e.g. Microsoft Corp" class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all" />
          </div>
          
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Shares</label>
              <input v-model="newAsset.quantity" type="number" step="0.0001" min="0" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all" />
            </div>
            
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Price ($)</label>
              <input v-model="newAsset.price" type="number" step="0.01" min="0" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition-all" />
            </div>
          </div>
          
          <div class="pt-4 flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-3 gap-y-3 sm:gap-y-0">
            <button type="button" @click="isAddModalOpen = false" class="w-full sm:w-auto px-5 py-2.5 text-sm font-medium text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors">
              Cancel
            </button>
            <button type="submit" :disabled="submitting" class="w-full sm:w-auto px-5 py-2.5 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 disabled:opacity-50 transition-colors flex items-center justify-center">
              <ArrowPathIcon v-if="submitting" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" />
              <span>Save Asset</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Edit Asset Modal Overlay -->
    <div v-if="isEditModalOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 dark:bg-black/80 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-md overflow-hidden flex flex-col transform transition-all border border-transparent dark:border-slate-800">
        <div class="flex items-center justify-between px-6 py-4 border-b border-slate-100 dark:border-slate-800 bg-amber-50 dark:bg-amber-950/20">
          <h3 class="text-lg font-bold text-slate-900 dark:text-white">Edit Asset</h3>
          <button @click="closeEditModal" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors rounded-full p-1 hover:bg-slate-200 dark:hover:bg-slate-700">
            <XMarkIcon class="h-6 w-6" />
          </button>
        </div>
        <form @submit.prevent="submitEdit" class="p-6 space-y-5">
          <div v-if="editError" class="bg-red-50 dark:bg-red-950/30 text-red-600 dark:text-red-400 text-sm p-3 rounded-lg border border-red-100 dark:border-red-900/50 flex items-start">
            <span class="mr-2">⚠️</span>
            <span>{{ editError }}</span>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Symbol</label>
              <input v-model="editForm.symbol" type="text" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500 outline-none transition-all uppercase" />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Company</label>
              <input v-model="editForm.name" type="text" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500 outline-none transition-all" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Shares</label>
              <input v-model="editForm.quantity" type="number" step="0.0001" min="0" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500 outline-none transition-all" />
            </div>
            <div>
              <label class="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-1">Price ($)</label>
              <input v-model="editForm.price" type="number" step="0.01" min="0" required class="w-full px-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 text-slate-900 dark:text-white rounded-lg focus:ring-2 focus:ring-amber-500 focus:border-amber-500 outline-none transition-all" />
            </div>
          </div>

          <div class="pt-4 flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-3 gap-y-3 sm:gap-y-0">
            <button type="button" @click="closeEditModal" class="w-full sm:w-auto px-5 py-2.5 text-sm font-medium text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 transition-colors">
              Cancel
            </button>
            <button type="submit" :disabled="editSubmitting" class="w-full sm:w-auto px-5 py-2.5 text-sm font-medium text-white bg-amber-500 rounded-lg hover:bg-amber-600 disabled:opacity-50 transition-colors flex items-center justify-center">
              <ArrowPathIcon v-if="editSubmitting" class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" />
              <span>Save Changes</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
