from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import yfinance as yf
import pandas as pd

app = FastAPI(title="Phoenix Quote Scraper")

# Allow CORS for local development testing
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
        
        # Check for price in common fields
        price = info.get('currentPrice', info.get('regularMarketPrice', info.get('price', 0)))
        name = info.get('shortName', info.get('longName', symbol))
        
        if price > 0:
            return {
                "symbol": symbol.upper(),
                "name": name,
                "price": float(price)
            }
        else:
            raise HTTPException(status_code=404, detail=f"No pricing data found for '{symbol}'")
            
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/api/history/{symbol}")
async def get_history(symbol: str):
    try:
        ticker = yf.Ticker(symbol)
        # Fetch 6 months to have enough data for SMAs, even if we show less
        hist = ticker.history(period="6mo")
        
        if hist.empty:
            raise HTTPException(status_code=404, detail=f"No historical data found for '{symbol}'")
            
        # Calculate Technical Indicators
        # SMA 20
        hist['SMA20'] = hist['Close'].rolling(window=20).mean()
        # SMA 50
        hist['SMA50'] = hist['Close'].rolling(window=50).mean()
        
        # Filter to last 2 months for the chart display to keep it clean
        display_data = hist.tail(60).copy()
        
        data = []
        for date, row in display_data.iterrows():
            item = {
                "date": date.strftime("%Y-%m-%d"),
                "price": float(row["Close"])
            }
            if not pd.isna(row['SMA20']):
                item["sma20"] = float(row['SMA20'])
            if not pd.isna(row['SMA50']):
                item["sma50"] = float(row['SMA50'])
                
            data.append(item)
            
        return data
            
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
