import { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';

function ChatPage() {
  const { chatId } = useParams();
  const [messages, setMessages] = useState([]);
  const [content, setContent] = useState('');
  const fileInputRef = useRef(null);

  useEffect(() => {
    const fetchMessages = async () => {
      try {
        const response = await fetch(`http://localhost:8080/api/Message/${chatId}`, {
          headers: {
            Authorization: `Bearer ${localStorage.getItem('token')}`,
          },
        });
        if (response.ok) {
          const data = await response.json();
          setMessages(data.messages || []);
        } else {
          throw new Error('Failed to fetch messages');
        }
      } catch (error) {
        console.error(error);
      }
    };
    fetchMessages();
  }, [chatId]);

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!content.trim()) return;

    try {
      const response = await fetch('http://localhost:8080/api/Message/text', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          chatId,
          content,
          contentType: 'Text',
          timeToLive: '00:00:00',
        }),
      });
      if (response.ok) {
        const newMessage = await response.json();
        setMessages([...messages, newMessage]);
        setContent('');
      } else {
        throw new Error('Failed to send message');
      }
    } catch (error) {
      console.error(error);
    }
  };

  const handleUploadFile = async (e) => {
    const file = e.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('ChatId', chatId);
    formData.append('File', file);
    formData.append('TimeToLive', '00:00:00');

    try {
      const response = await fetch('http://localhost:8080/api/Message/media', {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${localStorage.getItem('token')}`,
        },
        body: formData,
      });
      if (response.ok) {
        const newMessage = await response.json();
        setMessages([...messages, newMessage]);
        fileInputRef.current.value = null;
      } else {
        throw new Error('Failed to upload file');
      }
    } catch (error) {
      console.error(error);
    }
  };

  const handleAttachClick = () => {
    fileInputRef.current.click();
  };

  return (
    <div className="container">
      <h2>Chat {chatId}</h2>
      <div className="chat-messages">
        {messages.map((msg) => (
          <div
            key={msg.id}
            className={`message ${msg.senderId === JSON.parse(localStorage.getItem('user')).id ? 'sent' : 'received'}`}
          >
            {msg.contentType === 'Text' && <p>{msg.content}</p>}
            {msg.contentType === 'Image' && (
              <img
                src={`http://localhost:9000/telega/${msg.content}`}
                alt="media"
                onError={(e) => (e.target.src = 'https://via.placeholder.com/200')}
              />
            )}
            {msg.contentType === 'Video' && <video src={`http://localhost:9000/telega/${msg.content}`} controls />}
          </div>
        ))}
      </div>
      <form className="message-form" onSubmit={handleSendMessage}>
        <button type="button" className="attach-button" onClick={handleAttachClick}>
          <span className="attach-icon"></span>
        </button>
        <input
          type="file"
          ref={fileInputRef}
          onChange={handleUploadFile}
          hidden
          accept="image/*,video/*"
        />
        <input
          type="text"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          placeholder="Type a message..."
        />
        <button type="submit">Send</button>
      </form>
    </div>
  );
}

export default ChatPage;