import React, { createContext, useContext, useState, useEffect } from 'react';
import type { ReactNode } from 'react';

interface AuthContextType {
  token: string | null;
  userId: string | null;
  username: string | null;
  isAuthenticated: boolean;
  login: (token: string, userId: string, username: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(null);
  const [userId, setUserId] = useState<string | null>(null);
  const [username, setUsername] = useState<string | null>(null);

  useEffect(() => {
    const savedToken = localStorage.getItem('token');
    const savedUserId = localStorage.getItem('userId');
    const savedUsername = localStorage.getItem('username');

    if (savedToken && savedUserId && savedUsername) {
      setToken(savedToken);
      setUserId(savedUserId);
      setUsername(savedUsername);
    }
  }, []);

  const login = (token: string, userId: string, username: string) => {
    setToken(token);
    setUserId(userId);
    setUsername(username);
    localStorage.setItem('token', token);
    localStorage.setItem('userId', userId);
    localStorage.setItem('username', username);
  };

  const logout = () => {
    setToken(null);
    setUserId(null);
    setUsername(null);
    localStorage.removeItem('token');
    localStorage.removeItem('userId');
    localStorage.removeItem('username');
  };

  return (
    <AuthContext.Provider
      value={{
        token,
        userId,
        username,
        isAuthenticated: !!token,
        login,
        logout
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
