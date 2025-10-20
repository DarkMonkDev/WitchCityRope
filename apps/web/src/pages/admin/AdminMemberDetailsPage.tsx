import React, { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Container, Title, Breadcrumbs, Anchor, Tabs, Card, Alert, Text, Group, Badge } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import {
  useMemberDetails,
  useMemberEventHistory,
  useMemberVolunteerHistory,
  useMemberIncidents,
} from '../../lib/api/hooks/useMemberDetails'
import { MemberOverviewTab } from '../../components/members/MemberOverviewTab'
import { MemberVettingTab } from '../../components/members/MemberVettingTab'
import { MemberEventsTab } from '../../components/members/MemberEventsTab'
import { MemberVolunteerTab } from '../../components/members/MemberVolunteerTab'
import { MemberIncidentsTab } from '../../components/members/MemberIncidentsTab'

export const AdminMemberDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<string>('overview')

  // Fetch member details for page title
  const { data: memberDetails, isLoading, error } = useMemberDetails(id || '', !!id)

  // Fetch event history for tab badge count
  const { data: eventHistory } = useMemberEventHistory(id || '', 1, 1, !!id)

  // Fetch volunteer history for tab badge count and visibility
  const { data: volunteerHistory } = useMemberVolunteerHistory(id || '', 1, 1, !!id)
  const hasVolunteerSessions = volunteerHistory && volunteerHistory.totalCount > 0

  // Fetch incidents to determine if tab should be shown
  const { data: incidentsData } = useMemberIncidents(id || '', !!id)
  const hasIncidents = incidentsData && incidentsData.totalCount > 0

  if (!id) {
    return (
      <Container size="xl" py="md">
        <Alert icon={<IconAlertCircle size={16} />} color="red" title="Invalid Member ID">
          <Text>No member ID provided in URL.</Text>
        </Alert>
      </Container>
    )
  }

  return (
    <Container size="xl" py="md">
      {/* Breadcrumbs */}
      <Breadcrumbs mb="md">
        <Anchor onClick={() => navigate('/admin/members')} c="burgundy">
          Admin Members
        </Anchor>
        <Text c="dimmed">Member Details</Text>
      </Breadcrumbs>

      {/* Page Title with Role and Status Badges */}
      <Group justify="space-between" align="center" mb="xl" wrap="wrap">
        <Title order={1} c="burgundy">
          {isLoading
            ? 'Loading...'
            : error
              ? 'Member Details'
              : memberDetails?.sceneName || 'Member Details'}
        </Title>
        {memberDetails && !isLoading && !error && (
          <Group gap="xs">
            {/* Role Badges */}
            {memberDetails.role.split(',').map((role, index) => (
              <Badge key={index} color="blue" size="lg" variant="filled">
                {role.trim().toUpperCase()}
              </Badge>
            ))}
            {/* Status Badge */}
            <Badge
              color={memberDetails.isActive ? 'green' : 'gray'}
              size="lg"
              variant="filled"
            >
              {memberDetails.isActive ? 'ACTIVE' : 'INACTIVE'}
            </Badge>
          </Group>
        )}
      </Group>

      {/* Tabs matching EventForm style */}
      <Card shadow="md" radius="lg" p="xl" style={{ backgroundColor: 'white' }}>
        <Tabs
          value={activeTab}
          onChange={setActiveTab}
          variant="pills"
          radius="md"
        >
          <Tabs.List
            style={{
              backgroundColor: 'var(--mantine-color-gray-0)',
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              padding: 'var(--mantine-spacing-md)',
            }}
          >
            <Tabs.Tab value="overview">Overview</Tabs.Tab>
            <Tabs.Tab value="vetting">Vetting</Tabs.Tab>
            <Tabs.Tab value="events">Events ({eventHistory?.totalCount || 0})</Tabs.Tab>
            {hasVolunteerSessions && (
              <Tabs.Tab value="volunteer">Volunteer ({volunteerHistory?.totalCount || 0})</Tabs.Tab>
            )}
            {hasIncidents && (
              <Tabs.Tab value="incidents">Incidents ({incidentsData?.totalCount || 0})</Tabs.Tab>
            )}
          </Tabs.List>

          <Tabs.Panel value="overview" pt="xl">
            <MemberOverviewTab memberId={id} />
          </Tabs.Panel>

          <Tabs.Panel value="vetting" pt="xl">
            <MemberVettingTab
              memberId={id}
              userVettingStatus={memberDetails?.vettingStatusDisplay}
            />
          </Tabs.Panel>

          <Tabs.Panel value="events" pt="xl">
            <MemberEventsTab memberId={id} />
          </Tabs.Panel>

          {hasVolunteerSessions && (
            <Tabs.Panel value="volunteer" pt="xl">
              <MemberVolunteerTab memberId={id} />
            </Tabs.Panel>
          )}

          {hasIncidents && (
            <Tabs.Panel value="incidents" pt="xl">
              <MemberIncidentsTab memberId={id} />
            </Tabs.Panel>
          )}
        </Tabs>
      </Card>
    </Container>
  )
}
