from fastapi import FastAPI
import webscrap

app = FastAPI()

# Define a route
@app.get("/fgmeter")
async def read_root():
    try:
        meter = webscrap.getMeterValue()
        print(meter)
        return {"message": "FG Index Inserted into database successfully"}
    except:
        return {"message": "Insertion Failed"}

# if __name__ == "__main__":
#     import uvicorn
#     # Run the FastAPI app using Uvicorn
#     uvicorn.run(app, host="localhost", port=8000)
