Environment Specs
- 

- VMWare virtual machine
- Windows Server 2022 Standard x64
-   ![image](https://github.com/KaizerT/rabbitmq-discussion/assets/37116136/9d666084-93b9-4dbb-92dd-4d89b4d013a6)
- 32 GB RAM
- 2.3 Ghz CPU
- .Net 6.0.6 - Windows Server Hosting, Runtime
- RabbitMQ Server 3.13.3
- Erlang OTP 26.2.5


Running the test
- 

1. After cloning the repository, open the solution which opens both the server and client projects
2. Publish both projects using the existing publish profiles.
3. The published folder should be at the solution root, and will contain both the client and the server binaries.
4. Transfer the published binaries to your VM.
5. Modify the appsettings.json file in the 'Rabbit.Test.Web' folder. Under "BrokerConnection", modify the Host, Username, and Password for the rabbit login.
6. Create a regular IIS Website, default log in and non-TLS will suffce, and point the directory to the location of the 'Rabbit.Test.Web' folder.
7. Open a browser, navigate to the following address, http://localhost:{YourAssignedPort}/api/component/version. This should return a JSON with a version, this is to confirm that the website is running correctly.
8. Start the executable under the 'Rabbit.Test.Client' folder.
9. Fill out the host with the correct port you assigned when creating the IIS website in step 5.
10. Fill out the amount of clients you want to try out, to replicate the issue 1500-2000 clients should be made.

