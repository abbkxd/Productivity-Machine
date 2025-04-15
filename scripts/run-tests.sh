#!/bin/bash

# Productive Machine Test Runner
# This script runs the automated test suite for the Productive Machine application

echo "Running Productive Machine tests..."

# Navigate to the test project directory
cd "$(dirname "$0")/../src/ProductiveMachine.Tests"

# Run the tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Capture the exit code
EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo -e "\n✅ All tests passed successfully!"
else
    echo -e "\n❌ Some tests failed. See the output above for details."
fi

exit $EXIT_CODE 