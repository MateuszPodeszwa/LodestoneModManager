// Tiny global toast store. Use anywhere: const toast = useToast(); toast.success('Done', '…')
export type ToastKind = 'success' | 'error' | 'info'

export interface Toast {
  id: string
  title: string
  msg?: string
  kind: ToastKind
}

export function useToast() {
  const toasts = useState<Toast[]>('toasts', () => [])

  const remove = (id: string) => {
    toasts.value = toasts.value.filter((t) => t.id !== id)
  }

  const push = (title: string, msg = '', kind: ToastKind = 'success', ttl = 3200) => {
    const id = `${Date.now()}-${Math.random().toString(36).slice(2)}`
    toasts.value = [...toasts.value, { id, title, msg, kind }]
    if (import.meta.client) {
      setTimeout(() => remove(id), ttl)
    }
    return id
  }

  return {
    toasts,
    remove,
    push,
    success: (title: string, msg = '') => push(title, msg, 'success'),
    error: (title: string, msg = '') => push(title, msg, 'error'),
    info: (title: string, msg = '') => push(title, msg, 'info'),
  }
}
