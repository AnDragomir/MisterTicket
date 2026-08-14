// Mirrors UserDTO from the API.
export interface UserAccount {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  role: 'Client' | 'Organizer' | 'Admin';
}

// Mirrors AuthResponseDTO: returned by both register and login.
export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: UserAccount;
}

// Mirrors LoginDTO.
export interface LoginRequest {
  email: string;
  password: string;
}

// Mirrors RegisterDTO.
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}
