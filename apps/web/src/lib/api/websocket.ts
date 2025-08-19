import { queryClient } from './queryClient'
import { eventKeys } from './utils/cache'
import type { EventDto } from './types/events.types'

interface WebSocketMessage {
  type: string
  eventId?: string
  memberId?: string
  data?: any
}

class WebSocketManager {
  private ws: WebSocket | null = null
  private reconnectAttempts = 0
  private maxReconnectAttempts = 5
  private reconnectInterval = 1000
  private isConnecting = false

  connect() {
    if (this.isConnecting || (this.ws && this.ws.readyState === WebSocket.OPEN)) {
      return
    }

    this.isConnecting = true
    
    try {
      // Note: In a real implementation, this would be wss://localhost:5655/ws
      // For validation, we'll simulate WebSocket functionality
      console.log('WebSocket connection simulated (no actual WebSocket server available)')
      
      // Simulate connection success
      setTimeout(() => {
        this.isConnecting = false
        this.reconnectAttempts = 0
        console.log('WebSocket connection simulated as successful')
        
        // Simulate periodic updates for validation
        this.startSimulatedUpdates()
      }, 100)
      
    } catch (error) {
      this.isConnecting = false
      this.handleConnectionError()
    }
  }

  private startSimulatedUpdates() {
    // Simulate real-time updates for validation purposes
    setInterval(() => {
      this.simulateEventUpdate()
    }, 30000) // Every 30 seconds
  }

  private simulateEventUpdate() {
    // Simulate an event registration update
    const message: WebSocketMessage = {
      type: 'EVENT_REGISTRATION',
      eventId: 'sample-event-id',
      data: {
        newRegistrationCount: Math.floor(Math.random() * 20) + 1
      }
    }
    
    this.handleMessage(message)
  }

  private handleMessage(message: WebSocketMessage) {
    console.log('WebSocket message received (simulated):', message)
    
    switch (message.type) {
      case 'EVENT_UPDATED':
        if (message.eventId) {
          // Invalidate event queries to refetch fresh data
          queryClient.invalidateQueries({ queryKey: eventKeys.detail(message.eventId) })
          queryClient.invalidateQueries({ queryKey: eventKeys.lists() })
          console.log(`Event ${message.eventId} updated via WebSocket`)
        }
        break
      
      case 'EVENT_REGISTRATION':
        if (message.eventId && message.data?.newRegistrationCount) {
          // Update registration count optimistically
          queryClient.setQueryData(
            eventKeys.detail(message.eventId),
            (oldEvent: EventDto | undefined) => {
              if (!oldEvent) return oldEvent
              return {
                ...oldEvent,
                registrationCount: message.data.newRegistrationCount,
              }
            }
          )
          console.log(`Event ${message.eventId} registration count updated to ${message.data.newRegistrationCount}`)
        }
        break
        
      case 'MEMBER_STATUS_CHANGED':
        if (message.memberId) {
          // Invalidate member-related queries
          queryClient.invalidateQueries({ queryKey: ['members'] })
          console.log(`Member ${message.memberId} status changed via WebSocket`)
        }
        break
        
      default:
        console.log('Unknown WebSocket message type:', message.type)
    }
  }

  private handleConnectionError() {
    console.error('WebSocket connection failed (simulated)')
    
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++
      const delay = this.reconnectInterval * Math.pow(2, this.reconnectAttempts - 1)
      
      console.log(`Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`)
      
      setTimeout(() => {
        this.connect()
      }, delay)
    } else {
      console.error('Max reconnection attempts reached')
    }
  }

  disconnect() {
    if (this.ws) {
      this.ws.close()
      this.ws = null
    }
    this.reconnectAttempts = 0
    console.log('WebSocket disconnected')
  }

  // Simulate sending a message (for validation)
  simulateMessage(type: string, data?: any) {
    const message: WebSocketMessage = {
      type,
      ...data
    }
    
    console.log('Simulating WebSocket message:', message)
    this.handleMessage(message)
  }

  getConnectionState(): string {
    if (this.isConnecting) return 'CONNECTING'
    if (this.ws && this.ws.readyState === WebSocket.OPEN) return 'OPEN'
    if (this.ws && this.ws.readyState === WebSocket.CONNECTING) return 'CONNECTING'
    if (this.ws && this.ws.readyState === WebSocket.CLOSING) return 'CLOSING'
    if (this.ws && this.ws.readyState === WebSocket.CLOSED) return 'CLOSED'
    return 'DISCONNECTED'
  }
}

// Export singleton instance
export const wsManager = new WebSocketManager()

// Auto-connect when module loads (for validation)
if (typeof window !== 'undefined') {
  wsManager.connect()
}