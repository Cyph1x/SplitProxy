cd ConnectionUtils
echo compiling ConnecionUtils
mcs -target:library -out:ConnectionUtils.dll SockConnection.cs server.cs utils.cs client.cs ClientConnection.cs
echo compiled ConnecionUtils
cp ConnectionUtils.dll ../connection\ split\ server/
cp ConnectionUtils.dll ../connection\ split\ client/
cd ../connection\ split\ server/
echo compiling testserver
mcs /reference:ConnectionUtils.dll testserver.cs
cp testserver.exe ..
echo compiled testserver
cd ../connection\ split\ client/
echo compiling testclient
mcs /reference:ConnectionUtils.dll testclient.cs
cp testclient.exe ..
echo compiled testclient