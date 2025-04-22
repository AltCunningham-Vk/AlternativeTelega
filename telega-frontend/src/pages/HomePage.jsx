import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';

function HomePage({ setToken }) {
  const [chats, setChats] = useState([]);
  const [user, setUser] = useState(JSON.parse(localStorage.getItem('user') || '{}'));
  const navigate = useNavigate();

  useEffect(() => {
    const fetchChats = async () => {
      try {
        const response = await fetch('http://localhost:8080/api/Chat', {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`,
          },
        });
        if (response.ok) {
          const data = await response.json();
          setChats(data);
        } else {
          throw new Error('Failed to fetch chats');
        }
      } catch (error) {
        console.error(error);
        localStorage.removeItem('token');
        localStorage.removeItem('user');
        setToken(null);
        navigate('/login');
      }
    };
    fetchChats();
  }, [navigate, setToken]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    navigate('/login');
  };

  return (
    <div>
      <div className="header">
        <h1>Telega</h1>
        <div>
          <span>Welcome, {user.displayName || 'User'}!</span>
          <button onClick={handleLogout}>Logout</button>
        </div>
      </div>
      <div className="container">
        <h2>Your Chats</h2>
        <div className="chat-list">
          {chats.map((chat) => (
            <Link key={chat.id} to={`/chat/${chat.id}`} className="chat-item">
              {chat.name || `Chat ${chat.id}`}
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}

export default HomePage;