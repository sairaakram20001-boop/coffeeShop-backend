import axios from 'axios'

const envApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim()

const fallbackApiBases = ['http://sairaakram-001-site1.mtempurl.com', 'http://localhost:53691', 'https://localhost:53690']
const configuredApiBases = envApiBaseUrl && envApiBaseUrl.length > 0 ? [envApiBaseUrl, ...fallbackApiBases] : fallbackApiBases
const uniqueApiBases = Array.from(new Set(configuredApiBases.map((url) => url.replace(/\/$/, ''))))

let activeApiBaseIndex = 0
export const API_BASE_URL = uniqueApiBases[activeApiBaseIndex]

export const resolveImageUrl = (imageUrl?: string | null): string | undefined => {
  if (!imageUrl) return undefined
  if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) return imageUrl

  const normalized = imageUrl.startsWith('/') ? imageUrl : `/${imageUrl}`
  return `${uniqueApiBases[activeApiBaseIndex]}${normalized}`
}

export const api = axios.create({
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  config.baseURL = uniqueApiBases[activeApiBaseIndex]
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const isNetworkFailure = !error?.response
    const hasAnotherBase = activeApiBaseIndex < uniqueApiBases.length - 1
    const isRetriableRequest = Boolean(error?.config) && !error?.config?._retryWithFallbackBase

    if (isNetworkFailure && hasAnotherBase && isRetriableRequest) {
      activeApiBaseIndex += 1
      const nextConfig = {
        ...error.config,
        _retryWithFallbackBase: true,
        baseURL: uniqueApiBases[activeApiBaseIndex],
      }
      return api.request(nextConfig)
    }

    const status = error?.response?.status
    const message =
      error?.response?.data?.message ||
      error?.response?.data ||
      error?.message ||
      'Unexpected server error'

    window.dispatchEvent(
      new CustomEvent('api:error', {
        detail: { status, message },
      }),
    )

    return Promise.reject(error)
  },
)

export default api
