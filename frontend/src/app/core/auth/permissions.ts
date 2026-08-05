export const AppPermissions = {
  AuthMe: 'auth.me',
  AuthProfileWrite: 'auth.profile.write',
  AuthPasswordChange: 'auth.password.change',
  ProvidersRead: 'providers.read',
  ProvidersManage: 'providers.manage',
  WorkflowsRead: 'workflows.read',
  WorkflowsWrite: 'workflows.write',
  WorkflowsExecute: 'workflows.execute',
  ChatRead: 'chat.read',
  ChatWrite: 'chat.write',
  UsageRead: 'usage.read',
} as const;

export type AppPermission = (typeof AppPermissions)[keyof typeof AppPermissions];

export const AppRoles = {
  Admin: 'Admin',
  User: 'User',
} as const;
