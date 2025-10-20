/**
 * Admin Members Management Page
 *
 * SECURITY: This page requires Administrator role
 * - Route-level protection via adminLoader
 * - Component-level verification (defense-in-depth)
 *
 * Pattern: Copied EXACTLY from AdminVettingPage.tsx
 * Route: /admin/members
 */

import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Container, Title, Alert } from '@mantine/core';
import { IconLock } from '@tabler/icons-react';
import { MembersList } from '../../features/admin/members/components/MembersList';
import { useUser } from '../../stores/authStore';

export const AdminMembersPage: React.FC = () => {
  const navigate = useNavigate();
  const user = useUser();

  // Component-level role verification (defense-in-depth)
  useEffect(() => {
    if (user && user.role !== 'Administrator') {
      console.error(
        'AdminMembersPage: Unauthorized access attempt by non-admin user:',
        user.email
      );
      navigate('/unauthorized', { replace: true });
    }
  }, [user, navigate]);

  // Show error if somehow accessed without proper role
  if (!user || user.role !== 'Administrator') {
    return (
      <Container size="xl" py="xl">
        <Alert icon={<IconLock size={16} />} color="red" title="Access Denied">
          You do not have permission to access this page.
        </Alert>
      </Container>
    );
  }

  return (
    <Container size="xl" py="xl">
      {/* Header */}
      <Title
        order={1}
        mb="xl"
        style={{
          fontFamily: "'Montserrat', sans-serif",
          fontSize: '32px',
          fontWeight: 800,
          color: '#880124',
          textTransform: 'uppercase',
          letterSpacing: '-0.5px',
        }}
      >
        Member Management
      </Title>

      {/* Members List */}
      <MembersList />
    </Container>
  );
};
