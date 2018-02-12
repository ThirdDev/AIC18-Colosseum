cd files
java -Djava.awt.headless=true -jar ../AIC18-Server.jar --config="server.cfg" > server.out &
java -jar ../Client.jar 127.0.0.1 7099 00000000000000000000000000000000 1000 attack client.attack.cfg > client.attack.out &
java -jar ../Client.jar 127.0.0.1 7099 00000000000000000000000000000000 1000 defend client.defend.cfg > client.defend.out