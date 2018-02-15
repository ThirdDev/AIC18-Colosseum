#!/bin/bash

cd files
(java -Djava.awt.headless=true -jar ../AIC18-Server.jar --config=server.cfg &> server.out;
echo server exited;
kill $(ps aux | grep 'java' | awk '{print $2}');) &
(java -jar ../Client.jar 127.0.0.1 7099 00000000000000000000000000000000 1000 attack client.attack.cfg &> client.attack.out;
echo attack client exited;
kill $(ps aux | grep 'java' | awk '{print $2}');) &
(java -jar ../Client.jar 127.0.0.1 7099 00000000000000000000000000000000 1000 defend client.defend.cfg &> client.defend.out;
echo defend client exited;
kill $(ps aux | grep 'java' | awk '{print $2}');)