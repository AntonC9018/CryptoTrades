dotnet tool restore
:: Copy the libraries 
dotnet run --project .\tools\cli\package_install\copy --configuration Release -- CryptoTrades\Assets\lib
:: Run the unity editor
"C:\Program Files\Unity\Hub\Editor\Unity 2022.2.7f1\Editor\Unity.exe" -projectPath CryptoTrades