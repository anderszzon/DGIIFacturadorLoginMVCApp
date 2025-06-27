# DGIIFacturadorLoginMVCApp
 Portal Web con Login MVC 



Escribir pruebas unitarias para todos los métodos en #file:'ListaRestrictivaController.cs' , con NUnit y usando el método That() en la clase Asert y a los métodos que consumen entidades, asignarles valor a todas las propiedades, agregar las pruebas de los else de todos los métodos






Remove-Item -Recurse -Force ./TestResults/*; dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults; $f = Get-ChildItem -Recurse -Filter coverage.cobertura.xml -Path ./TestResults | Select-Object -First 1; if ($f) { Copy-Item $f.FullName -Destination "./TestResults/coverage.cobertura.xml" -Force; Write-Host "✅ Cobertura generada en TestResults/coverage.cobertura.xml" } else { Write-Host "❌ No se encontró el archivo de cobertura" }; reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:InformeCobertura





Remove-Item -Recurse -Force ./TestResults/*; dotnet test --settings ../coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./TestResults; $f = Get-ChildItem -Recurse -Filter coverage.cobertura.xml -Path ./TestResults | Select-Object -First 1; if ($f) { Copy-Item $f.FullName -Destination "./TestResults/coverage.cobertura.xml" -Force; Write-Host "✅ Cobertura generada en TestResults/coverage.cobertura.xml" } else { Write-Host "❌ No se encontró el archivo de cobertura" }; reportgenerator -reports:TestResults/coverage.cobertura.xml -targetdir:InformeCobertura -assemblyfilters:-*Program




git rm -r --cached .vs/
git rm -r --cached */bin/
git rm -r --cached */obj/
git add .gitignore
git commit -m "Excluir archivos generados de compilación"
