from flask import Flask, render_template, request, jsonify
import symbolUnderOneStrategy
import strategyPerformance

app = Flask(__name__)

@app.route("/")
def homepage():
    return render_template("heatMapHome.html")

@app.route("/generate", methods=["POST"])
def generate_heat_map():
    strategy = request.form.get("strategy")
    symbol = request.form.get("symbol")
    button_action = request.form.get('button_action')
    print(button_action)
    print("Selected Strategy:", strategy)
    # print("Symbol:", symbol)
    if button_action=="AsymbolInAStrategy":
        fig = symbolUnderOneStrategy.fncsymbolUnderAStrategy(strategy, symbol)
        return render_template('generatedHeatmap.html', heatmap=fig.to_html(full_html=False))
    elif button_action=="symbolsInAStrategy":
        fig = strategyPerformance.strategyPerformanceForSymbols(strategy)
        return render_template('generatedHeatmap.html', heatmap=fig.to_html(full_html=False))


if __name__ == "__main__":
    app.run(debug=True)
