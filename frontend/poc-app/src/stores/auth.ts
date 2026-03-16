import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useRouter } from 'vue-router'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('jwt_token') || null)
  const router = useRouter()

  const setToken = (newToken: string) => {
    token.value = newToken
    localStorage.setItem('jwt_token', newToken)
  }

  const clearToken = () => {
    token.value = null
    localStorage.removeItem('jwt_token')
  }

  const logout = () => {
    clearToken()
    router.push('/')
  }

  return { token, setToken, clearToken, logout }
})
