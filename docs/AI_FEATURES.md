# AI Features Documentation

## Overview
HardwareShopPro integrates Claude AI (v3.5 Sonnet) to provide intelligent assistance directly within the application. The AI Assistant helps users analyze data, find products, and get business insights using natural language.

## Features
### 1. AI Assistant Chat
- **Interface**: A dedicated chat view accessible from the sidebar.
- **Capabilities**:
  - **Smart Product Search**: Ask "Find all 16GB RAM under Rs. 5000" to filter inventory. (Integration via `SmartSearchAsync`)
  - **Sales Insights**: Ask "How were sales last week?" for a summary.
  - **General Assistance**: Get help with application features or hardware knowledge.
- **Offline Handling**: Gracefully degrades when internet or API key is missing.

### 2. Configuration (`SettingsView`)
- **API Key**: Secure input for Claude API Key.
- **Model Selection**: Choose between available Claude models (default: `claude-sonnet-4-20250514`).
- **Toggle**: Enable/Disable AI features globally.

## Technical Implementation
### Service: `ClaudeAIService`
- Implements `IAIService` interface.
- Handles HTTP requests to Anthropic API.
- Retries on transient failures (Exponential Backoff).
- Maps natural language queries to structured `SearchCriteria` JSON.

### ViewModel: `AIAssistantViewModel`
- Manages chat history (`ObservableCollection<ChatMessage>`).
- Routes user queries to appropriate AI service methods.
- Provides suggestion prompts for common tasks.
