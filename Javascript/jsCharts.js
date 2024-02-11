function summaryChart(filterValue) {
	console.log(filterValue);
	if (chart) {
		chart.destroy();
	}
	if(filterValue==='1d'){
		return;
	}
	$.ajax({
		url: '@Url.Action("GetProfitLossSummary", "Home")',
		type: 'GET',
		data: { dateRange: filterValue},
		success: function (result) {
			var chartData = result.map(item => ({
				x: item["dtDate"],
				y: item["pL_Sum"]
			}));
			var options = {

				chart: {
					type: 'line',
					height: 300,
					toolbar: {
						show: false
					}
				},
				plotOptions: {
					line: {
						curve: 'stepline'
					}
				},
				dataLabels: {
					enabled: true
				},
				series: [
					{
						name: 'Close',
						data: chartData
					}
				],
				xaxis: {
					type: 'datetime',
					labels: {
						format: 'dd MMM yy'
					}
				},
				yaxis: {
					title: {
						text: 'Close'
					}
				},
				fill: {
					type: 'gradient',
					gradient: {
						shadeIntensity: 1,
						inverseColors: false,
						opacityFrom: 0.7,
						opacityTo: 0.2,
						stops: [0, 100]
					}
				}
			};

			chart = new ApexCharts(document.querySelector("#summaryChart"), options);
			chart.render();
		},
		error: function (error) {
			console.log("Ajax triggering Failed");
		}
	})
}
