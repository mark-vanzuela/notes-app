// The signed-in user, mirroring the API's UserDto.
export interface User {
  id: string;
  email: string;
  name: string;
  pictureUrl: string | null;
}

// The response from POST /api/auth/google (the API's AuthResultDto): our app's JWT,
// when it expires, and the user's profile.
export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  user: User;
}
