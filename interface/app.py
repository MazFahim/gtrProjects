from fastapi import FastAPI, Request
from fastapi.responses import HTMLResponse
from fastapi.templating import Jinja2Templates
from fastapi.staticfiles import StaticFiles
import localDb
import uvicorn

 
app = FastAPI()

templates = Jinja2Templates(directory="templates")
app.mount("/static", StaticFiles(directory="templates/static"), name="static")

@app.get("/backtest/", response_class=HTMLResponse)
def automatedBacktest(request: Request):
    context = {'request': request}
    return templates.TemplateResponse("automatedBacktest.html", context)

@app.post("/submit")
async def submit_form(request: Request):
    try:
        form_data = await request.form()
        # Process the form data
        name = form_data.get("name")
        dtFrom = form_data.get("dtFrom")
        dtTo = form_data.get("dtTo")
        longLow = form_data.get("longLow")
        longHigh = form_data.get("longHigh")
        shortLow = form_data.get("shortLow")
        shortHigh = form_data.get("shortHigh")
        option = form_data.get("option")
        print(dtFrom)
        #print(type(dtFrom))
        rtrn = localDb.fncCheckForDuplicate(dtFrom, dtTo, longLow, longHigh, shortLow, shortHigh)
        return {"message": "Form submitted successfully"}
    except Exception as ex:
        return {"message": "Data already exist in the tables"}

if __name__ == "__main__":
    uvicorn.run("app:app", host="localhost", port=8000, reload=True)