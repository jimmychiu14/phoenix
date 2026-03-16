from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import yfinance as yf

app = FastAPI(title="Phoenix Quote Scraper")

# Allow CORS for local development testing if needed directly
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/api/quote/{symbol}")
async def get_quote(symbol: str):
    try:
        ticker = yf.Ticker(symbol)
        info = ticker.info
        
        # Check if we got valid data back by looking for a price field
        if 'regularMarketPrice' in info or 'currentPrice' in info:
            price = info.get('currentPrice', info.get('regularMarketPrice', 0))
            name = info.get('shortName', info.get('longName', symbol))
            
            return {
                "symbol": symbol.upper(),
                "name": name,
                "price": float(price)
            }
        else:
            raise HTTPException(status_code=404, detail=f"No pricing data found for symbol '{symbol}'")
            
    except Exception as e:
        # yfinance can throw broad exceptions on failure
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/history/{symbol}")
async def get_history(symbol: str):
    try:
        ticker = yf.Ticker(symbol)
        hist = ticker.history(period="1mo")
        
        if hist.empty:
            raise HTTPException(status_code=404, detail=f"No historical data found for symbol '{symbol}'")
            
        data = []
        for date, row in hist.iterrows():
            data.append({
                "date": date.strftime("%Y-%m-%d"),
                "price": float(row["Close"])
            })
            
        return data
            
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
