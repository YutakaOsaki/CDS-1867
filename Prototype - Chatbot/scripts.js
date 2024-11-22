const chatWindow = document.getElementById('chat-window');
const transcriptDisplay = document.getElementById('transcript-display');
const micButton = document.getElementById('mic-button');
let recognition;
let isRecognizing = false;

if ('webkitSpeechRecognition' in window) {
    recognition = new webkitSpeechRecognition();
    recognition.lang = 'en-US';
    recognition.continuous = true;
    recognition.interimResults = false;

    recognition.onresult = function(event) {
        const transcript = event.results[0][0].transcript;
        transcriptDisplay.innerText = transcript;
        sendMessage();
    };

    recognition.onerror = function(event) {
        console.error('Recognition error:', event.error);
        stopRecognition();
    };

    recognition.onend = function() {
        console.log('Recognition ended.');
        if (isRecognizing) {
            recognition.start();
        }
    };
}

function sendMessage() {
    const message = transcriptDisplay.innerText.trim();
    if (message === "") return;

    const timestamp = new Date().toISOString();
    appendMessage('user-message', message, timestamp);
    transcriptDisplay.innerText = '';

    try {
        fetch('https://0631q0ohzb.execute-api.ap-northeast-1.amazonaws.com/prod/chatbot', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ message }),
        })
        .then(response => response.json())
        .then(data => {
            console.log(data);
            const messageText = JSON.parse(data.body);
            const botTimestamp = JSON.parse(data.TimeStamp);
            appendMessage('bot-message', messageText, botTimestamp);
        })
        .catch(error => {
            console.error('Fetch error:', error);
            appendMessage('bot-message', 'An error occurred. Please try again.', new Date().toISOString());
        });
    } catch (error) {
        console.error('Fetch error:', error);
        appendMessage('bot-message', 'An error occurred. Please try again.', new Date().toISOString());
    }
}

function appendMessage(className, message, timestamp) {
    const messageElement = document.createElement('div');
    messageElement.className = `chat-message ${className}`;
    messageElement.id = `message-${timestamp}`;
    messageElement.innerHTML = `
        ${className === 'bot-message' ? '<div class="bot-avatar"><img src="images/Angelus.png" alt="Avatar"></div><div class="chat___"><span class="bot-name">Angelus Novus</span><div class="bot-content">' : ''}
        <p>${message}</p>
        ${className === 'bot-message' ? '</div></div>' : ''}
    `;
    chatWindow.appendChild(messageElement);
    chatWindow.scrollTop = chatWindow.scrollHeight;

    // Add Go Back button to bot messages
    if (className === 'bot-message') {
        const goBackButton = document.createElement('button');
        goBackButton.className = 'go-back-button';
        goBackButton.innerText = 'Go Back';
        goBackButton.onclick = function(event) {
            event.stopPropagation();
            goBack(timestamp);
        };
        messageElement.appendChild(goBackButton);

        // Add event listener for showing the Go Back button
        messageElement.onclick = function() {
            showGoBackButton(messageElement);
        };
    }
}

function toggleRecognition() {
  if (isRecognizing) {
      stopRecognition();
      micButton.innerHTML = '<i class="fas fa-microphone"></i>'; // Change to mic icon
      micButton.classList.remove('recording');
  } else {
      startRecognition();
      micButton.innerHTML = '<i class="fas fa-stop-circle"></i>'; // Change to stop icon
      micButton.classList.add('recording');
  }
}

function startRecognition() {
    if (recognition) {
        recognition.start();
        isRecognizing = true;
    }
}

function stopRecognition() {
    if (recognition) {
        recognition.stop();
        isRecognizing = false;
    }
}

function showGoBackButton(messageElement) {
    const goBackButton = messageElement.querySelector('.go-back-button');
    if (goBackButton) {
        goBackButton.classList.add('show');
        
        // Hide the button after 5 seconds if not clicked
        setTimeout(() => {
            if (goBackButton.classList.contains('show')) {
                goBackButton.classList.remove('show');
            }
        }, 5000);
    }
}

function goBack(timestamp) {
    fetch('https://ugryb2xkw8.execute-api.ap-northeast-1.amazonaws.com/prod/goback', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ TimeStamp: timestamp }),
    })
    .then(response => response.json())
    .then(data => {
        console.log('Go Back response:', data);
    })
    .catch(error => {
        console.error('Fetch error:', error);
    });
}
