export interface Event {
  id: string
  title: string
  shortDescription?: string // Brief summary for card displays
  description: string
  startDate: string // ISO 8601 string
  location: string
}
