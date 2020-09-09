#!/bin/sh
export GIT_COMMITTER_NAME="Committer Name"
export GIT_COMMITTER_EMAIL="committer@address.com"
export GIT_AUTHOR_NAME="Author Name"
export GIT_AUTHOR_EMAIL="author@address.com"
export GIT_COMMITTER_DATE="2020-01-01T12:00:00+00:00"
export GIT_AUTHOR_DATE="2020-01-01T12:00:00+00:00"

rm -rf fork1/ upstream/

mkdir upstream
cd upstream
git init .

cp ../version.json .
git add version.json

git commit --message "Initial commit."

echo "Gallia est omnis divisa in partes tres, quarum unam incolunt Belgae, aliam Aquitani, tertiam qui ipsorum lingua Celtae, nostra Galli appellantur.\n" > de_bello_gallico.txt
git add de_bello_gallico.txt
git commit --message "Add first phrase"
git gc --prune=now

echo "Hi omnes lingua, institutis, legibus inter se differunt. Gallos ab Aquitanis Garumna flumen, a Belgis Matrona et Sequana dividit..\n" >> de_bello_gallico.txt
git add de_bello_gallico.txt
git commit --message "Add second phrase"

git checkout -b chapters/2
echo "Apud Helvetios longe nobilissimus fuit et ditissimus Orgetorix. Is M. Messala, [et P.] M. Pisone consulibus regni cupiditate inductus coniurationem nobilitatis fecit et civitati persuasit ut de finibus suis cum omnibus copiis exirent:\n" >> de_bello_gallico_2.txt
git add de_bello_gallico_2.txt
git commit --message "Add first phrase"

echo "perfacile esse, cum virtute omnibus praestarent, totius Galliae imperio potiri." >> de_bello_gallico_2.txt
git add de_bello_gallico_2.txt
git commit --message "Add second phrase"

echo "Id hoc facilius iis persuasit, quod undique loci natura Helvetii continentur: una ex parte flumine Rheno latissimo atque altissimo, qui agrum Helvetium a Germanis dividit; altera ex parte monte Iura altissimo, qui est inter Sequanos et Helvetios; tertia lacu Lemanno et flumine Rhodano, qui provinciam nostram ab Helvetiis dividit.\n" >> de_bello_gallico_2.txt
git add de_bello_gallico_2.txt
git commit --message "Add third phrase"

git checkout master
git checkout -b versions/2.0
sed -i 's/1.1/2.0/' version.json

git add version.json
git commit -m "Bump to version 2.0"

cd ..
git clone --shared --branch master upstream fork1
cd fork1

echo "Horum omnium fortissimi sunt Belgae, propterea quod a cultu atque humanitate provinciae longissime absunt, minimeque ad eos mercatores saepe commeant atque ea quae ad effeminandos animos pertinent important," >> de_bello_gallico.txt
git add de_bello_gallico.txt
git commit --message "Add third phrase"

echo "proximique sunt Germanis, qui trans Rhenum incolunt, quibuscum continenter bellum gerunt. Qua de causa Helvetii quoque reliquos Gallos virtute praecedunt, quod fere cotidianis proeliis cum Germanis contendunt, cum aut suis finibus eos prohibent aut ipsi in eorum finibus bellum gerunt." >> de_bello_gallico.txt
git add de_bello_gallico.txt
git commit --message "Add fourth phrase"
git gc --prune=now

git merge origin/versions/2.0 --message "Merge version 2.0"

git merge origin/chapters/2 --message "Merge chapter 2"

cd ..

