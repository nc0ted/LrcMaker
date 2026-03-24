# LrcMaker

A Blazor WASM application for generating synced `.lrc` files via the Groq API.

## Features
- API key stored in `localStorage`.
- Transcription via Whisper (Groq).
- Automatic text-to-timestamp alignment.

## Getting a Groq API Key
1. Visit the [Groq Console](https://console.groq.com/).
2. Sign in or create an account.
3. Go to **API Keys** and click **Create API Key**.
4. Copy the key and use it in the application.

## Usage
1. Enter your **Groq API Key** and click **Save**.
2. Select an **audio file** (mp3, wav, m4a, mp4, webm, etc.).
3. Paste **lyrics** into the text field.
4. Click **Generate** and download the result.

## License
MIT
