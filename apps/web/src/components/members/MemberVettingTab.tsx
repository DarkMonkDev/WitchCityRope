import React from 'react'
import { Stack, Title, Card, Text, Paper, Alert, Grid } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { useMemberVetting } from '../../lib/api/hooks/useMemberDetails'

interface MemberVettingTabProps {
  memberId: string
  userVettingStatus?: string
}

export const MemberVettingTab: React.FC<MemberVettingTabProps> = ({ memberId, userVettingStatus }) => {
  const { data: vettingDetails, isLoading, error } = useMemberVetting(memberId)

  if (isLoading) {
    return (
      <Paper p="xl">
        <Text ta="center" c="dimmed">
          Loading vetting details...
        </Text>
      </Paper>
    )
  }

  if (error) {
    return (
      <Alert icon={<IconAlertCircle size={16} />} color="red" title="Error">
        <Text>Failed to load vetting details: {(error as Error).message}</Text>
      </Alert>
    )
  }

  // Format date helper
  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A'
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    })
  }

  // Get the last status change with status name and date
  const getLastStatusChangeDate = () => {
    if (!vettingDetails || !vettingDetails.hasApplication) {
      return 'Application Not Submitted'
    }

    // Use the User's actual vetting status (source of truth) instead of the application's workflow status
    const statusName = userVettingStatus || vettingDetails.workflowStatusDisplay || 'Unknown Status'

    // Determine which date to use
    let changeDate: string | undefined

    // Use lastReviewedAt if available (tracks status changes)
    if (vettingDetails.lastReviewedAt) {
      changeDate = vettingDetails.lastReviewedAt
    }
    // Fall back to decisionMadeAt if a final decision was made
    else if (vettingDetails.decisionMadeAt) {
      changeDate = vettingDetails.decisionMadeAt
    }
    // If no status change yet, use submission date
    else if (vettingDetails.submittedAt) {
      changeDate = vettingDetails.submittedAt
    }

    if (!changeDate) {
      return `${statusName} - N/A`
    }

    return `${statusName} - ${formatDate(changeDate)}`
  }

  const getDateApplied = () => {
    if (!vettingDetails || !vettingDetails.hasApplication) {
      return 'Application Not Submitted'
    }
    return formatDate(vettingDetails.submittedAt)
  }

  return (
    <Stack gap="xl">
      {/* Application Information Section - Always show */}
      <div>
        <Title
          order={2}
          c="burgundy"
          mb="md"
          style={{
            borderBottom: '2px solid var(--mantine-color-burgundy-3)',
            paddingBottom: '8px',
          }}
        >
          Application Information
        </Title>
        <Card withBorder p="md" radius="md">
          <Grid>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Date Applied
              </Text>
              <Text fw={600}>{getDateApplied()}</Text>
            </Grid.Col>
            <Grid.Col span={6}>
              <Text size="sm" c="dimmed" mb={4}>
                Last Status Change
              </Text>
              <Text fw={600}>{getLastStatusChangeDate()}</Text>
            </Grid.Col>
          </Grid>
        </Card>
      </div>

      {/* Show message if no application submitted */}
      {(!vettingDetails || !vettingDetails.hasApplication) && (
        <Alert icon={<IconAlertCircle size={16} />} color="blue" title="No Application">
          <Text>This member has not submitted a vetting application yet.</Text>
        </Alert>
      )}

      {/* Questionnaire Responses - Only show if application exists */}
      {vettingDetails && vettingDetails.hasApplication && (
        <div>
          <Title
            order={2}
            c="burgundy"
            mb="md"
            style={{
              borderBottom: '2px solid var(--mantine-color-burgundy-3)',
              paddingBottom: '8px',
            }}
          >
            Vetting Application Responses
          </Title>
          <Stack gap="md">
            {/* Other Names or Handles - optional field from form */}
            {vettingDetails.otherNames && (
              <Card withBorder p="md" radius="md">
                <Text fw={600} mb="xs">
                  Other Names or Handles
                </Text>
                <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                  {vettingDetails.otherNames}
                </Text>
              </Card>
            )}

            {/* Why Join - required field from form */}
            <Card withBorder p="md" radius="md">
              <Text fw={600} mb="xs">
                Why would you like to join Witch City Rope
              </Text>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                {vettingDetails.whyJoin || vettingDetails.whyJoinCommunity || 'No response provided'}
              </Text>
            </Card>

            {/* Experience with Rope - required field from form */}
            <Card withBorder p="md" radius="md">
              <Text fw={600} mb="xs">
                Experience with Rope
              </Text>
              <Text size="sm" style={{ whiteSpace: 'pre-wrap' }}>
                {vettingDetails.experienceWithRope || vettingDetails.experienceDescription || 'No response provided'}
              </Text>
            </Card>
          </Stack>
        </div>
      )}
    </Stack>
  )
}
