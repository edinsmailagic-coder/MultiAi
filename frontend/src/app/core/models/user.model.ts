export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  userName: string;
  createdAt: string;
  roles?: string[];
  permissions?: string[];
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
