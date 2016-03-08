#define _WINSOCK_DEPRECATED_NO_WARNINGS

#include<stdio.h>
#include<winsock2.h>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

#define SERVER "127.0.0.1"  //ip address of udp server
#define PORT 8888   //The port on which to listen for incoming data
#define BUFLEN 512  //Max length of buffer

/**
	Program for sending passed command line parameters to UDP listening socket.
	Receiving socket is hard-coded for performance reasons.
	Program executes in about 11ms (measure with PowerShell command: "Measure-Command")
	with and without server listening and uses very little memory. It is thus well 
	suited to be called by Windows Scheduler whenever a block connection was logged.
*/
int wmain(int argc, wchar_t* argv[]) //use Microsoft's wmain to get wide chars.
{
	struct sockaddr_in si_other;
	int s, slen = sizeof(si_other);
	char buf[BUFLEN];
	char message[BUFLEN];
	WSADATA wsa;

	//Initialise winsock
	//printf("\nInitialising Winsock...");
	if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
	{
		printf("Failed. Error Code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}
	//printf("Initialised.\n");

	//create socket
	if ((s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP)) == SOCKET_ERROR)
	{
		printf("socket() failed with error code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}

	//setup address structure
	memset((char *)&si_other, 0, sizeof(si_other));
	si_other.sin_family = AF_INET;
	si_other.sin_port = htons(PORT);
	si_other.sin_addr.S_un.S_addr = inet_addr(SERVER);

	//get command line
	LPTSTR cmd = GetCommandLine();
	size_t   i;
	int len = wcslen(cmd);

	//extract parameters from command line
	if (argc == 1) {
		sprintf_s(message, "No argument passed.");
	}
	else if (len < BUFLEN)
	{
		wcstombs_s(&i, buf, BUFLEN, cmd, BUFLEN);
		int l = wcslen(argv[0]);
		char* arg = buf;
		if (arg[0] == '\"') {
			arg += l + 2; //wcslen does not count enclosing quotes, add explicitly if needed
		}
		else {
			arg += l;
		}
		sprintf_s(message, "%s (%i)", arg, l);
	}
	else {
		sprintf_s(message, "Too long argument passed. (%i>=%i)", len, BUFLEN);
	}

	//send the message
	if (sendto(s, message, strlen(message), 0, (struct sockaddr *) &si_other, slen) == SOCKET_ERROR)
	{
		printf("sendto() failed with error code : %d", WSAGetLastError());
		exit(EXIT_FAILURE);
	}

	closesocket(s);
	WSACleanup();
	return 0;
}