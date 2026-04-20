import axios from 'axios'
import { useAuthStore } from './stores/auth'

const api = axios.create({
  baseURL: 'https://phoenix-yv46.onrender.com/api',
})

api.interceptors.request.use(
  (config) => {
    const authStore = useAuthStore()
    if (authStore.token) {
      config.headers.Authorization = `Bearer ${authStore.token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

export default api
