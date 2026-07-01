---
description: You have to use this agent if you want to run the testsuite or if the user asks you to run unit tests.
mode: subagent
model: opencode/big-pickle
temperature: 0.1
tools:
    questions: false
permissions:
    skill:
        "*": deny
    task:
        "*": deny
        "explore": allow
    bash:
        "*": allow
---

**Instructions**
1. Run unit tests with "dotnet test"
2. Try to fix potential issues
3. If all the tests pass output an instruction for the agent that invoked you to say that they all passed
4. If after a certain amount of attempts you can't fix the issues then just give back an instruction of everything you tried so a next agent can try again, or the human can try
