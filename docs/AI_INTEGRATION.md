# AI Integration

NepalKhata leverages Artificial Intelligence to provide features that go beyond standard management software.

## Anthropic Claude Integration

The application integrates with the **Anthropic Claude API** (specifically `claude-3-sonnet` or `claude-3.5-sonnet`) through the `ClaudeAIService`.

### Key AI Features

#### 1. Smart Search (Natural Language Queries)
Users can search for products using vague or descriptive terms. 
- *Input:* "What should I use to stick heavy wood together?"
- *AI Response:* Interprets the intent and filters inventory for "Wood Glue", "Araldite", or "Bonding Agents".

#### 2. Reorder Alerts & Suggestions (Planned)
The AI analyzes stock levels and historical sales to suggest reorder quantities and identify slow-moving items.

#### 3. Sales Insights (Planned)
Automatic generation of weekly summaries explaining which products are trending and offering advice on stock positioning.

### Implementation Details

- **Graceful Fallback:** If the user is offline or the API key is invalid, the system automatically falls back to standard keyword-based searching.
- **Retry Logic:** Implemented exponential backoff for API calls to handle rate limits or transient network failures.
- **Asynchronous Processing:** All AI operations are non-blocking to ensure the UI remains responsive during network calls.

### Configuration

API settings are managed in `appsettings.json`:
```json
"AI": {
  "ApiKey": "YOUR_KEY_HERE",
  "Model": "claude-3.5-sonnet-20240620",
  "MaxRetries": 3
}
```
*Note: In production, the API Key is encrypted using the Windows Data Protection API.*
