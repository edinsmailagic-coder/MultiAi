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
  UsersRead: 'users.read',
  UsersManage: 'users.manage',
  RolesRead: 'roles.read',
  RolesManage: 'roles.manage',
} as const;

export type AppPermission = (typeof AppPermissions)[keyof typeof AppPermissions];

export const AppRoles = {
  Admin: 'Admin',
  Moderator: 'Moderator',
  User: 'User',
} as const;
