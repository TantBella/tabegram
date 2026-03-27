import client from './client';
import type { AuthResponse } from '../types';

export const authApi = {
  register: async (username: string, password: string): Promise<AuthResponse> => {
    const res = await client.post('/auth/register', { username, password });
    return res.data;
  },

  login: async (username: string, password: string): Promise<AuthResponse> => {
    const res = await client.post('/auth/login', { username, password });
    return res.data;
  }
};
