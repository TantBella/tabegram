import React from 'react';
import { Navbar as BSNavbar, Container, Nav, Button } from 'react-bootstrap';
import { useAuth } from '../auth/AuthContext';
import { useNavigate } from 'react-router-dom';

export const Navbar: React.FC = () => {
  const { isAuthenticated, username, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <BSNavbar bg="light" expand="lg" className="border-bottom">
      <Container>
        <BSNavbar.Brand href="/" className="fw-bold" style={{ color: '#f5c400' }}>
          🌼 Tabegram
        </BSNavbar.Brand>
        <BSNavbar.Toggle aria-controls="basic-navbar-nav" />
        <BSNavbar.Collapse id="basic-navbar-nav">
          <Nav className="ms-auto">
            {isAuthenticated ? (
              <>
                <Nav.Link href="/">Feed</Nav.Link>
                <Nav.Link href="/new-post">New Post</Nav.Link>
                <Nav.Link href={`/profile/${localStorage.getItem('userId')}`}>
                  Profile ({username})
                </Nav.Link>
                <Button variant="outline-danger" size="sm" onClick={handleLogout}>
                  Logout
                </Button>
              </>
            ) : (
              <>
                <Nav.Link href="/login">Login</Nav.Link>
                <Nav.Link href="/register">Register</Nav.Link>
              </>
            )}
          </Nav>
        </BSNavbar.Collapse>
      </Container>
    </BSNavbar>
  );
};
