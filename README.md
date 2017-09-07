PassData Demo
===========

Pass data in a signed PE using the unauthenticated attributes field of the PKCS#7 signature 

## Projects

* Demo Application<br>Source EXE downloaded to the client.  Signed using an msbuild task that runs `AfterBuild`.  To demo, enter your code-signing thumbprint into `Demo Application.csproj`.  Uses ILMerge to allow single-file execution.
* Demo Website<br>Example website that stamps the download time into the exe as an example
* Demo.Common<br>Contains the sentinels used to pass data from the server to the exe
* PassData.ClientSide<br>Code used to extract the data from the exe when running on the client
* PassData.ServerSide<br>Code used to stamp the data into the exe

## Interesting bits

Client side retrieval of data (Demo Application\MainForm.cs)

    try
    {
        var thisPath = Assembly.GetExecutingAssembly().Location;
        var stampData = StampReader.ReadStampFromFile(thisPath, StampConstants.StampSubject, StampConstants.StampOid);
        var stampText = Encoding.UTF8.GetString(stampData);

        lbStamped.Text = stampText;
    }
    catch (StampNotFoundException ex)
    {
        MessageBox.Show(this, $"Could not locate stamp\r\n\r\n{ex.Message}", Text);
    }

Server side stamping (Demo Website\Controllers\HomeController.cs)

    var stampText = $"Server time is currently {DateTime.Now} at time of stamping";
    var stampData = Encoding.UTF8.GetBytes(stampText);
    var sourceFile = Server.MapPath("~/Content/Demo Application.exe");
    var signToolPath = Server.MapPath("~/App_Data/signtool.exe");
    var tempFile = Path.GetTempFileName();
    bool deleteStreamOpened = false;
    try
    {
        IOFile.Copy(sourceFile, tempFile, true);
        StampWriter.StampFile(tempFile, signToolPath, StampConstants.StampSubject, StampConstants.StampOid, stampData);

        var deleteOnClose = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, FileOptions.DeleteOnClose);
        deleteStreamOpened = true;
        return File(deleteOnClose, "application/octet-stream", "Demo Application.exe");
    }
    finally
    {
        if (!deleteStreamOpened)
        {
            try
            {
                IOFile.Delete(tempFile);
            }
            catch
            {
                // no-op, opportunistic cleanup
                Debug.WriteLine("Failed to cleanup file");
            }
        }
    }
