# ?? Chat System Client Implementation Guide

## ?? Frontend JavaScript (Angular/React/Vanilla JS)

### 1. **Install SignalR Client**
```bash
npm install @microsoft/signalr
```

### 2. **Chat Service Implementation**
```typescript
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export class ChatService {
    private hubConnection: HubConnection | null = null;
  private isConnected = false;

  constructor(private authToken: string) {}

    // ?? K?t n?i t?i ChatHub
    async connect(): Promise<boolean> {
        try {
    this.hubConnection = new HubConnectionBuilder()
              .withUrl('https://localhost:7100/chathub', {
    accessTokenFactory: () => this.authToken
        })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
              .build();

            // ?? L?ng nghe tin nh?n m?i
            this.hubConnection.on('ReceiveMessage', (messageData) => {
         this.handleNewMessage(messageData);
            });

            // ? Xác nh?n tin nh?n ?ã g?i
   this.hubConnection.on('MessageSent', (confirmation) => {
    console.log('Message sent successfully:', confirmation);
    });

            // ? L?i g?i tin nh?n
      this.hubConnection.on('MessageError', (error) => {
                console.error('Message error:', error);
this.showError(error);
       });

     // ?? Tin nh?n ?ã ??c
            this.hubConnection.on('MessageRead', (readInfo) => {
         this.handleMessageRead(readInfo);
        });

       // ?? Typing indicators
    this.hubConnection.on('UserStartTyping', (typingInfo) => {
   this.showTypingIndicator(typingInfo);
       });

            this.hubConnection.on('UserStopTyping', (typingInfo) => {
     this.hideTypingIndicator(typingInfo.UserId);
      });

      // ?? Chat session events
          this.hubConnection.on('JoinedChatSession', (sessionInfo) => {
      console.log('Joined chat session:', sessionInfo);
            });

          this.hubConnection.on('ChatSessionEnded', (endInfo) => {
  this.handleChatSessionEnded(endInfo);
            });

      // ?? Connection events
            this.hubConnection.onreconnecting((error) => {
         console.log('Reconnecting...', error);
           this.isConnected = false;
            });

        this.hubConnection.onreconnected((connectionId) => {
            console.log('Reconnected with ID:', connectionId);
        this.isConnected = true;
     });

            this.hubConnection.onclose((error) => {
    console.log('Connection closed:', error);
      this.isConnected = false;
     });

        await this.hubConnection.start();
            this.isConnected = true;
 console.log('? Connected to ChatHub');
 return true;

        } catch (error) {
            console.error('? Failed to connect to ChatHub:', error);
          return false;
        }
    }

    // ?? G?i tin nh?n
    async sendMessage(recipientId: number, message: string, chatSessionId?: number): Promise<boolean> {
        if (!this.isConnected || !this.hubConnection) {
     console.error('Hub not connected');
       return false;
        }

        try {
            await this.hubConnection.invoke('SendMessage', recipientId, message, chatSessionId);
            return true;
   } catch (error) {
  console.error('Failed to send message:', error);
     return false;
        }
    }

    // ????? Join chat session
    async joinChatSession(chatSessionId: number): Promise<boolean> {
        if (!this.isConnected || !this.hubConnection) return false;

        try {
      await this.hubConnection.invoke('JoinChatSession', chatSessionId);
            return true;
   } catch (error) {
     console.error('Failed to join chat session:', error);
            return false;
        }
    }

    // ?? Typing indicators
    async startTyping(recipientId: number): Promise<void> {
      if (this.isConnected && this.hubConnection) {
            await this.hubConnection.invoke('StartTyping', recipientId);
 }
    }

    async stopTyping(recipientId: number): Promise<void> {
        if (this.isConnected && this.hubConnection) {
            await this.hubConnection.invoke('StopTyping', recipientId);
 }
    }

    // ?? Mark message as read
    async markMessageAsRead(messageId: string): Promise<void> {
  if (this.isConnected && this.hubConnection) {
        await this.hubConnection.invoke('MarkMessageAsRead', messageId);
        }
}

    // ?? Ng?t k?t n?i
    async disconnect(): Promise<void> {
        if (this.hubConnection) {
            await this.hubConnection.stop();
            this.isConnected = false;
        }
    }

    // Event handlers
    private handleNewMessage(messageData: any): void {
        console.log('New message received:', messageData);
      // Update UI with new message
    this.addMessageToUI(messageData);
    }

    private handleMessageRead(readInfo: any): void {
        console.log('Message read:', readInfo);
 // Update UI to show message as read
        this.markMessageAsReadInUI(readInfo.MessageId);
    }

    private showTypingIndicator(typingInfo: any): void {
   console.log('User typing:', typingInfo);
        // Show typing indicator in UI
    }

  private hideTypingIndicator(userId: number): void {
        console.log('User stopped typing:', userId);
  // Hide typing indicator in UI
    }

    private handleChatSessionEnded(endInfo: any): void {
        console.log('Chat session ended:', endInfo);
        // Handle session end in UI
    }

    private addMessageToUI(messageData: any): void {
  // Implementation depends on your UI framework
    const chatContainer = document.getElementById('chat-messages');
     if (chatContainer) {
   const messageElement = this.createMessageElement(messageData);
            chatContainer.appendChild(messageElement);
 chatContainer.scrollTop = chatContainer.scrollHeight;
 }
    }

    private createMessageElement(messageData: any): HTMLElement {
 const messageDiv = document.createElement('div');
        messageDiv.className = `message ${messageData.SenderId === this.currentUserId ? 'sent' : 'received'}`;
        messageDiv.innerHTML = `
       <div class="message-header">
   <span class="sender">${messageData.SenderName}</span>
             <span class="timestamp">${new Date(messageData.Timestamp).toLocaleTimeString()}</span>
            </div>
    <div class="message-content">${messageData.Message}</div>
      `;
        return messageDiv;
    }

    private markMessageAsReadInUI(messageId: string): void {
      const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
      if (messageElement) {
 messageElement.classList.add('read');
        }
    }

    private showError(error: string): void {
    // Show error message in UI
        console.error('Chat error:', error);
    }
}
```

### 3. **Chat Component HTML**
```html
<div class="chat-container">
    <!-- Chat header -->
    <div class="chat-header">
    <h3>?? Chat v?i Admin</h3>
        <div class="connection-status" [class.connected]="isConnected">
   {{ isConnected ? '?ang k?t n?i' : 'M?t k?t n?i' }}
        </div>
    </div>

    <!-- Messages area -->
    <div class="chat-messages" #chatMessages>
        <div *ngFor="let message of messages" 
      class="message" 
      [class.sent]="message.SenderId === currentUserId"
             [class.received]="message.SenderId !== currentUserId"
       [attr.data-message-id]="message.Id">
            
          <div class="message-header">
        <span class="sender">{{ message.SenderName }}</span>
         <span class="timestamp">{{ message.Timestamp | date:'short' }}</span>
   <span *ngIf="message.IsRead" class="read-indicator">??</span>
        </div>
        <div class="message-content">{{ message.Content }}</div>
    </div>

 <!-- Typing indicator -->
      <div *ngIf="isTyping" class="typing-indicator">
            <span>{{ typingUserName }} ?ang so?n tin...</span>
        </div>
    </div>

    <!-- Input area -->
    <div class="chat-input">
     <div class="input-group">
       <input type="text" 
            class="form-control" 
          placeholder="Nh?p tin nh?n..."
  [(ngModel)]="newMessage"
         (keyup.enter)="sendMessage()"
        (keyup)="onTyping()"
      #messageInput>
      <button class="btn btn-primary" 
           (click)="sendMessage()"
    [disabled]="!isConnected || !newMessage.trim()">
                ?? G?i
            </button>
        </div>
    </div>
</div>
```

### 4. **CSS Styles**
```css
.chat-container {
    height: 500px;
    display: flex;
    flex-direction: column;
    border: 1px solid #ddd;
    border-radius: 8px;
    background: white;
}

.chat-header {
    padding: 15px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 8px 8px 0 0;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.connection-status {
    padding: 4px 8px;
    border-radius: 12px;
    background: rgba(255,255,255,0.2);
    font-size: 12px;
}

.connection-status.connected {
    background: rgba(0,255,0,0.3);
}

.chat-messages {
    flex: 1;
overflow-y: auto;
    padding: 15px;
    background: #f8f9fa;
}

.message {
    margin-bottom: 15px;
    max-width: 80%;
    animation: messageSlideIn 0.3s ease-out;
}

.message.sent {
    margin-left: auto;
text-align: right;
}

.message.received {
    margin-right: auto;
}

.message-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 5px;
    font-size: 12px;
    color: #666;
}

.message.sent .message-header {
    justify-content: flex-end;
}

.message-content {
    padding: 10px 15px;
    border-radius: 18px;
    background: white;
    box-shadow: 0 1px 2px rgba(0,0,0,0.1);
    word-wrap: break-word;
}

.message.sent .message-content {
    background: #007bff;
    color: white;
}

.message.received .message-content {
    background: white;
    color: #333;
}

.read-indicator {
    color: #007bff;
    font-size: 10px;
}

.typing-indicator {
    margin: 10px 0;
    font-style: italic;
    color: #666;
    animation: pulse 1.5s infinite;
}

.chat-input {
    padding: 15px;
border-top: 1px solid #ddd;
    background: white;
    border-radius: 0 0 8px 8px;
}

.input-group {
    display: flex;
    gap: 10px;
}

.input-group input {
    flex: 1;
    padding: 10px 15px;
    border: 1px solid #ddd;
    border-radius: 20px;
    outline: none;
}

.input-group input:focus {
    border-color: #007bff;
}

.input-group button {
    padding: 10px 20px;
    border: none;
    border-radius: 20px;
    background: #007bff;
    color: white;
    cursor: pointer;
    white-space: nowrap;
}

.input-group button:disabled {
    background: #ccc;
    cursor: not-allowed;
}

@keyframes messageSlideIn {
from {
   opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

@keyframes pulse {
    0%, 100% { opacity: 1; }
    50% { opacity: 0.5; }
}

/* Mobile responsive */
@media (max-width: 768px) {
    .chat-container {
        height: 400px;
    }
    
    .message {
   max-width: 90%;
    }
    
    .input-group button {
    padding: 10px 15px;
        font-size: 14px;
  }
}
```

## ?? WinForms Client Implementation

### **C# WinForms SignalR Client**
```csharp
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

public partial class ChatForm : Form
{
    private HubConnection? _hubConnection;
    private string _jwtToken;
    private int _currentUserId;

    public ChatForm(string jwtToken, int userId)
    {
        InitializeComponent();
    _jwtToken = jwtToken;
      _currentUserId = userId;
    }

    private async void ChatForm_Load(object sender, EventArgs e)
    {
        await ConnectToHub();
    }

    private async Task ConnectToHub()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
     .WithUrl("https://localhost:7100/chathub", options =>
   {
              options.AccessTokenProvider = () => Task.FromResult(_jwtToken)!;
       })
                .WithAutomaticReconnect()
    .Build();

    // Event handlers
            _hubConnection.On<object>("ReceiveMessage", OnMessageReceived);
    _hubConnection.On<object>("MessageSent", OnMessageSent);
          _hubConnection.On<string>("MessageError", OnMessageError);
    _hubConnection.On<object>("UserStartTyping", OnUserStartTyping);
       _hubConnection.On<object>("UserStopTyping", OnUserStopTyping);

            _hubConnection.Reconnecting += (error) =>
            {
   this.Invoke(() =>
         {
        lblStatus.Text = "?ang k?t n?i l?i...";
       lblStatus.ForeColor = Color.Orange;
           });
             return Task.CompletedTask;
     };

            _hubConnection.Reconnected += (connectionId) =>
        {
 this.Invoke(() =>
       {
                    lblStatus.Text = "?ã k?t n?i";
            lblStatus.ForeColor = Color.Green;
                });
                return Task.CompletedTask;
            };

      _hubConnection.Closed += (error) =>
 {
   this.Invoke(() =>
    {
    lblStatus.Text = "M?t k?t n?i";
  lblStatus.ForeColor = Color.Red;
        });
        return Task.CompletedTask;
   };

  await _hubConnection.StartAsync();
  
            lblStatus.Text = "?ã k?t n?i";
            lblStatus.ForeColor = Color.Green;
       btnSend.Enabled = true;
        }
        catch (Exception ex)
        {
     MessageBox.Show($"L?i k?t n?i: {ex.Message}", "L?i", MessageBoxButtons.OK, MessageBoxIcon.Error);
    lblStatus.Text = "L?i k?t n?i";
            lblStatus.ForeColor = Color.Red;
        }
    }

    private void OnMessageReceived(object messageData)
    {
        this.Invoke(() =>
 {
     // Parse messageData and add to chat display
            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(messageData.ToString()!);
            string displayText = $"[{DateTime.Parse(message.Timestamp.ToString()!):HH:mm}] {message.SenderName}: {message.Message}";
            
            rtbChatHistory.AppendText(displayText + Environment.NewLine);
            rtbChatHistory.ScrollToCaret();
        });
    }

    private void OnMessageSent(object confirmation)
    {
        this.Invoke(() =>
    {
   txtMessage.Clear();
            txtMessage.Focus();
        });
    }

    private void OnMessageError(string error)
    {
     this.Invoke(() =>
        {
    MessageBox.Show($"L?i g?i tin nh?n: {error}", "L?i", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        });
    }

    private void OnUserStartTyping(object typingInfo)
    {
        this.Invoke(() =>
   {
          lblTyping.Text = "Ng??i dùng ?ang so?n tin...";
            lblTyping.Visible = true;
     });
    }

    private void OnUserStopTyping(object typingInfo)
    {
  this.Invoke(() =>
        {
          lblTyping.Visible = false;
        });
    }

    private async void btnSend_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtMessage.Text) || _hubConnection == null)
         return;

        try
        {
        // recipientId = user ID (get from selected user), 0 = all users
            int recipientId = (int)cbUsers.SelectedValue;
            await _hubConnection.SendAsync("SendMessage", recipientId, txtMessage.Text.Trim());
        }
        catch (Exception ex)
        {
MessageBox.Show($"L?i g?i tin nh?n: {ex.Message}", "L?i", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ChatForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (_hubConnection != null)
   {
      await _hubConnection.StopAsync();
    await _hubConnection.DisposeAsync();
        }
    }
}
```

## ?? API Integration Examples

### **Send Message via REST API (alternative to SignalR)**
```typescript
async sendMessageViaAPI(recipientId: number, message: string): Promise<any> {
    const response = await fetch('/api/Message/send', {
method: 'POST',
  headers: {
          'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.authToken}`
        },
        body: JSON.stringify({
   recipientId: recipientId,
     message: message
        })
    });
    
    return await response.json();
}

// Get message history
async getMessageHistory(chatSessionId: number, page: number = 1): Promise<any> {
    const response = await fetch(`/api/Message/history/${chatSessionId}?pageNumber=${page}`, {
        headers: {
    'Authorization': `Bearer ${this.authToken}`
        }
    });
    
    return await response.json();
}

// Get chat sessions
async getChatSessions(): Promise<any> {
    const response = await fetch('/api/Message/chat-sessions', {
      headers: {
      'Authorization': `Bearer ${this.authToken}`
        }
    });
  
    return await response.json();
}
```

## ?? Testing & Debugging

### **Test SignalR Connection**
```javascript
// Browser console test
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub", {
    accessTokenFactory: () => "your-jwt-token-here"
    })
    .build();

connection.start().then(function () {
    console.log("? Connected!");
    
    // Test send message
    connection.invoke("SendMessage", 0, "Test message from console");
}).catch(function (err) {
    console.error("? Connection failed:", err);
});
```

H? th?ng Chat Realtime hoàn ch?nh v?i SignalR ?ã s?n sàng! ??

**Tính n?ng chính:**
- ? Realtime messaging
- ? JWT Authentication
- ? Typing indicators  
- ? Read receipts
- ? Chat sessions
- ? Admin support
- ? WinForms integration
- ? Error handling
- ? Auto-reconnect