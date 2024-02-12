# Use an official Python runtime as a parent image
FROM python:3.10

# Set the working directory in the container to /app
WORKDIR /app

# Copy the requirements.txt file into the container at /app
COPY ./requirements.txt /app/requirements.txt

# Install the required packages using pip
RUN pip install --no-cache-dir --upgrade -r /app/requirements.txt

# Copy the current directory contents into the container at /app
COPY . /app

# Expose the port that Flask will run on (usually 5000)
# EXPOSE 5000

# Define the command to run your FastApi application
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "80"]
